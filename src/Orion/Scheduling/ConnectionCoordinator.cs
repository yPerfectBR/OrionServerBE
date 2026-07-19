using Orion.Player;
using Orion.Protocol.Packets;
using Orion.RakNet;
using Orion.Scheduling.Messages;

namespace Orion.Scheduling;

public sealed class ConnectionCoordinator
{
    private readonly Server _server;
    private readonly SessionWorkerPool _pool;

    public ConnectionCoordinator(Server server, SessionWorkerPool pool)
    {
        _server = server;
        _pool = pool;
    }

    public bool IsActive => _server.Properties.SessionThreadingEnabled;

    public SessionWorkerPool Pool => _pool;

    public void Start() => _pool.Start();

    public void Stop() => _pool.Stop();

    public void AssignSession(PlayerSession session)
    {
        int workerId = ResolveWorkerId(session.Connection);
        session.SessionWorkerId = workerId;
        _pool.GetWorker(workerId).RegisterSession(session);
    }

    public void ReleaseSession(PlayerSession session)
    {
        if (session.SessionWorkerId is int workerId)
        {
            _pool.GetWorker(workerId).UnregisterSession(session.Connection);
        }

        session.SessionWorkerId = null;
    }

    public void EnqueueSessionPacket(NetworkConnection connection, Orion.Protocol.Enums.PacketId packetId, ReadOnlySpan<byte> payload)
    {
        if (!_server.Sessions.TryGetValue(connection, out PlayerSession? session))
        {
            return;
        }

        int workerId = session.SessionWorkerId ?? ResolveWorkerId(connection);
        _pool.GetWorker(workerId).Enqueue(new SessionPacketMessage
        {
            Connection = connection,
            PacketId = packetId,
            Payload = payload.ToArray()
        });
    }

    public void EnqueueViewDelta(PlayerSession session, DataPacket packet)
    {
        if (session.SessionWorkerId is not int workerId)
        {
            Network.SessionSendCoordinator.SendDirect(session, packet);
            return;
        }

        _pool.GetWorker(workerId).Enqueue(new ViewDeltaMessage
        {
            Session = session,
            Packet = packet
        });
    }

    public void EnqueueSend(PlayerSession session, IReadOnlyList<DataPacket> packets)
    {
        if (packets.Count == 0)
        {
            return;
        }

        int workerId = session.SessionWorkerId ?? ResolveWorkerId(session.Connection);
        SessionWorker worker = _pool.GetWorker(workerId);

        if (worker.IsCurrentThread())
        {
            Network.SessionSendCoordinator.SendDirect(session, packets);
            return;
        }

        for (int i = 0; i < packets.Count; i++)
        {
            worker.Enqueue(new ViewDeltaMessage
            {
                Session = session,
                Packet = packets[i]
            });
        }
    }

    public void RunOnSessionThread(PlayerSession session, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        int workerId = session.SessionWorkerId ?? ResolveWorkerId(session.Connection);
        SessionWorker worker = _pool.GetWorker(workerId);

        if (worker.IsCurrentThread())
        {
            action();
            return;
        }

        TaskCompletionSource<object?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Enqueue(new RunOnSessionThreadMessage
        {
            Action = action,
            Completion = completion
        });
        completion.Task.GetAwaiter().GetResult();
    }

    int ResolveWorkerId(NetworkConnection connection) =>
        Math.Abs(connection.GetHashCode()) % _pool.WorkerCount;
}
