using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Orion.Config;
using Orion.RakNet.Packets;
using Orion.RakNet.Packets.Types;

namespace Orion.RakNet;

public sealed class NetworkServer : IDisposable
{
    private const int RakNetHeaderSize = 28;
    private const int DefaultFrameBufferSize = 2048;
    private const int DisconnectTimeoutMs = 15000;

    public RaknetServerOptions Options { get; }
    public IPEndPoint? LocalEndPoint => _socket?.LocalEndPoint as IPEndPoint;

    public readonly ArrayPool<byte> FramesPool = ArrayPool<byte>.Create(
        maxArrayLength: DefaultFrameBufferSize,
        maxArraysPerBucket: 4096
    );

    public event Action<NetworkConnection>? OnConnected;
    public event Action<NetworkConnection>? OnDisconnected;
    public event Action<NetworkConnection, ReadOnlyMemory<byte>>? OnMessage;

    public ulong ServerGuid => unchecked((ulong)OrionInfo.ServerGuid);

    private readonly byte[] _cookieSecret = RandomNumberGenerator.GetBytes(32);
    private readonly ConcurrentDictionary<EndpointKey, NetworkServerConnection> _connections = new();
    private readonly int _frameBufferSize;

    private Socket? _socket;
    private CancellationTokenSource? _runCts;

    public NetworkServer(RaknetServerOptions options = default)
    {
        RaknetServerOptions resolvedOptions = options.Equals(default(RaknetServerOptions))
            ? RaknetServerOptions.FromOrionInfo()
            : options;

        ushort normalizedMaxMtu = Math.Clamp(resolvedOptions.MaxMtu, resolvedOptions.MinMtu, ushort.MaxValue);
        if (normalizedMaxMtu != resolvedOptions.MaxMtu)
        {
            resolvedOptions = resolvedOptions with { MaxMtu = normalizedMaxMtu };
        }

        Options = resolvedOptions;
        _frameBufferSize = Math.Max(DefaultFrameBufferSize, (int)Options.MaxMtu);
    }

    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[_frameBufferSize];

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        if (OperatingSystem.IsWindows())
        {
            const int sioUdpConnReset = -1744830452;
            _socket.IOControl(sioUdpConnReset, [0, 0, 0, 0], null);
        }

        _socket.Bind(new IPEndPoint(IPAddress.Any, Options.PortIpv4));

        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        CancellationToken token = _runCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                SocketAddress endpoint = new(AddressFamily.InterNetwork);

                try
                {
                    int received = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, endpoint, token);

                    if (received > 0)
                    {
                        ReceiveFrom(CloneEndpoint(endpoint), buffer.AsSpan(0, received));
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }
        finally
        {
            try { _socket?.Close(); } catch { }
        }
    }

    public void Stop()
    {
        _runCts?.Cancel();
        try { _socket?.Close(); } catch { }
    }

    public void Dispose()
    {
        Stop();
        _runCts?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void ReceiveFrom(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (_socket is null || message.Length == 0 || message.Length > _frameBufferSize)
        {
            return;
        }

        try
        {
            switch (message[0])
            {
                case UnconnectedPing.PacketId:
                case UnconnectedPing.OpenConnectionsPacketId:
                    HandleUnconnectedPing(endpoint, message);
                    break;

                case OpenConnectionRequestOne.PacketId:
                    HandleOpenConnectionRequestOne(endpoint, message);
                    break;

                case OpenConnectionRequestTwo.PacketId:
                    HandleOpenConnectionRequestTwo(endpoint, message);
                    break;

                case >= 0x80 and <= 0x8d:
                    HandleFrameSet(endpoint, message);
                    break;

                case Ack.PacketId:
                    HandleAck(endpoint, message);
                    break;

                case Nack.PacketId:
                    HandleNack(endpoint, message);
                    break;

                case DisconnectNotification.PacketId:
                    HandleDisconnectNotification(endpoint, message);
                    break;
            }
        }
        catch
        {
            // Malformed UDP packets are normal on public ports.
        }
    }

    private void HandleFrameSet(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (message.Length < 4)
        {
            return;
        }

        EndpointKey key = new(endpoint);

        if (!_connections.TryGetValue(key, out NetworkServerConnection? connection))
        {
            return;
        }

        connection.HandleFrameSet(FrameSet.Deserialize(message));
    }

    private void HandleAck(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (message.Length < 4)
        {
            return;
        }

        EndpointKey key = new(endpoint);

        if (_connections.TryGetValue(key, out NetworkServerConnection? connection))
        {
            connection.HandleAck(Ack.Deserialize(message));
        }
    }

    private void HandleNack(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (message.Length < 4)
        {
            return;
        }

        EndpointKey key = new(endpoint);

        if (_connections.TryGetValue(key, out NetworkServerConnection? connection))
        {
            connection.HandleNack(Nack.Deserialize(message));
        }
    }

    private void HandleDisconnectNotification(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (message.Length < 1)
        {
            return;
        }

        EndpointKey key = new(endpoint);
        if (_connections.TryGetValue(key, out NetworkServerConnection? connection))
        {
            connection.Disconnect(false);
        }
    }

    private void HandleUnconnectedPing(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (message.Length < 1 + sizeof(long) + Magic.MAGIC_LENGTH)
        {
            return;
        }

        UnconnectedPing ping = UnconnectedPing.Deserialize(message);
        string advertisement = OrionInfo.BuildRaknetAdvertisement();
        UnconnectedPong pong = new(ping.Time, ServerGuid, advertisement);

        SendTo(endpoint, pong, UnconnectedPong.Serialize);
    }

    private void HandleOpenConnectionRequestOne(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (message.Length < 1 + Magic.MAGIC_LENGTH + 1)
        {
            return;
        }

        ushort mtu = (ushort)Math.Clamp(message.Length + RakNetHeaderSize, Options.MinMtu, Options.MaxMtu);

        uint? cookie = Options.EnableCookies
            ? ConnectionCookie.Create(endpoint, _cookieSecret)
            : null;

        OpenConnectionReplyOne reply = new((long)ServerGuid, cookie, mtu);
        SendTo(endpoint, reply, OpenConnectionReplyOne.Serialize);
    }

    private void HandleOpenConnectionRequestTwo(SocketAddress endpoint, ReadOnlySpan<byte> message)
    {
        if (_socket is null || _connections.Count >= Options.MaxConnections || message.Length < 1 + Magic.MAGIC_LENGTH)
        {
            return;
        }

        OpenConnectionRequestTwo request = OpenConnectionRequestTwo.Deserialize(message);

        if (Options.EnableCookies &&
            (!request.Cookie.HasValue || !ConnectionCookie.Validate(endpoint, _cookieSecret, request.Cookie.Value)))
        {
            return;
        }

        SocketAddress connectionEndpoint = CloneEndpoint(endpoint);
        EndpointKey connectionKey = new(connectionEndpoint);

        if (_connections.ContainsKey(connectionKey))
        {
            return;
        }

        ushort selectedMtu = Math.Clamp(request.MTU, Options.MinMtu, Options.MaxMtu);

        OpenConnectionReplyTwo reply = new((long)ServerGuid, endpoint, selectedMtu, false);
        SendTo(endpoint, reply, OpenConnectionReplyTwo.Serialize);

        NetworkServerConnection connection = new(_socket, connectionEndpoint, request.ClientId, selectedMtu);

        connection.Connected += connected => OnConnected?.Invoke(connected);

        connection.Disconnected += disconnected =>
        {
            if (disconnected is NetworkServerConnection serverConnection)
            {
                _connections.TryRemove(new EndpointKey(serverConnection.Endpoint), out _);
            }

            OnDisconnected?.Invoke(disconnected);
        };

        connection.Message += (source, payload) => OnMessage?.Invoke(source, payload);

        _connections.TryAdd(connectionKey, connection);
    }

    private void SendTo<T>(SocketAddress endpoint, T packet, Func<T, Span<byte>, int> serializer)
    {
        if (_socket is null)
        {
            return;
        }

        byte[] buffer = FramesPool.Rent(DefaultFrameBufferSize);

        try
        {
            int length = serializer(packet, buffer);

            if (length <= 0 || length > buffer.Length)
            {
                return;
            }

            _socket.SendTo(buffer.AsSpan(0, length), SocketFlags.None, endpoint);
        }
        finally
        {
            FramesPool.Return(buffer);
        }
    }

    public void Tick()
    {
        long now = Environment.TickCount64;
        foreach (NetworkServerConnection connection in _connections.Values)
        {
            if (connection.IsConnected && now - connection.LastSeenMs >= DisconnectTimeoutMs)
            {
                connection.Disconnect();
                continue;
            }

            connection.Tick(now);
        }
    }

    private static SocketAddress CloneEndpoint(SocketAddress endpoint)
    {
        SocketAddress clone = new(endpoint.Family, endpoint.Size);

        for (int i = 0; i < endpoint.Size; i++)
        {
            clone[i] = endpoint[i];
        }

        return clone;
    }

    private readonly struct EndpointKey : IEquatable<EndpointKey>
    {
        private readonly AddressFamily _family;
        private readonly byte[] _bytes;

        public EndpointKey(SocketAddress endpoint)
        {
            _family = endpoint.Family;
            _bytes = endpoint.Buffer.Span[..endpoint.Size].ToArray();
        }

        public bool Equals(EndpointKey other)
        {
            return _family == other._family &&
                   _bytes.AsSpan().SequenceEqual(other._bytes);
        }

        public override bool Equals(object? obj)
        {
            return obj is EndpointKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            HashCode hash = new();

            hash.Add((int)_family);
            hash.AddBytes(_bytes);

            return hash.ToHashCode();
        }
    }
}
