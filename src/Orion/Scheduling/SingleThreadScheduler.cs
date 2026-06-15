using System.Collections.Concurrent;
using System.Diagnostics;
using Orion.Config;
using Log = Orion.Logger.Logger;
using Orion.Protocol.Enums;
using Orion.RakNet;
using Orion.Scheduling.Messages;

namespace Orion.Scheduling;

public sealed class SingleThreadScheduler : INetworkScheduler
{
    private const int MaxMessagesPerDrain = 256;

    private readonly Server _server;
    private readonly ConcurrentQueue<INetworkQueueMessage> _mainQueue = new();
    private int _simulationThreadId;

    public SingleThreadScheduler(Server server) => _server = server;

    public void Start()
    {
    }

    public void Stop() => DrainMainQueueCore(int.MaxValue);

    internal int PendingMessageCount => _mainQueue.Count;

    public IReadOnlyList<WorkerLoadMetrics> GetMetrics() =>
    [
        new WorkerLoadMetrics
        {
            WorkerId = 0,
            ActiveWorldCount = _server.World is null ? 0 : 1,
            TotalPresentPlayers = _server.Sessions.Count,
            Tps = _server.Tps
        }
    ];

    public void EnqueueGamePacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> payload)
    {
        _mainQueue.Enqueue(new ProcessPacketMessage
        {
            Connection = connection,
            PacketId = packetId,
            Payload = payload.ToArray()
        });
    }

    public void EnqueueDisconnect(NetworkConnection connection)
    {
        _mainQueue.Enqueue(new ProcessDisconnectMessage
        {
            Connection = connection
        });
    }

    public void DrainMainQueue() => DrainMainQueueCore(MaxMessagesPerDrain);

    void DrainMainQueueCore(int maxMessages)
    {
#if DEBUG
        ThreadGuard.CurrentWorkerId = 0;
#endif

        _simulationThreadId = Thread.CurrentThread.ManagedThreadId;
        int processed = 0;
        while (processed < maxMessages && _mainQueue.TryDequeue(out INetworkQueueMessage? message))
        {
            processed++;
            try
            {
                switch (message)
                {
                    case RunOnMainThreadMessage runMessage:
                        HandleRunOnMainThread(runMessage);
                        break;

                    case ProcessPacketMessage packetMessage:
                        if (_server.Properties.WorldSchedulerDebug)
                        {
                            Log.Debug(
                                LogCategory.Orion,
                                "[Scheduler] ProcessPacketMessage packet={0}",
                                packetMessage.PacketId);
                        }

                        _server.Network.HandleGamePacketOnWorker(
                            packetMessage.Connection,
                            packetMessage.PacketId,
                            packetMessage.Payload);
                        break;

                    case ProcessDisconnectMessage disconnectMessage:
                        if (_server.Properties.WorldSchedulerDebug)
                        {
                            Log.Debug(LogCategory.Orion, "[Scheduler] ProcessDisconnectMessage");
                        }

                        _server.Network.ProcessDisconnectOnWorker(disconnectMessage.Connection);
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.Warn(LogCategory.Orion, "Scheduler message error: {0}", exception.Message);
            }
        }
    }

    static void HandleRunOnMainThread(RunOnMainThreadMessage message)
    {
        try
        {
            message.Action();
            message.Completion?.TrySetResult(null);
        }
        catch (Exception exception)
        {
            message.Completion?.TrySetException(exception);
            throw;
        }
    }
}
