using System.Collections.Concurrent;
using System.Diagnostics;
using Orion.Config;
using Orion.Network;
using Orion.Player;
using Orion.Player.Traits;
using Orion.Scheduling.Messages;
using Log = Orion.Logger.Logger;

namespace Orion.Scheduling;

public sealed class SessionWorker
{
    private const int MaxMessagesPerDrain = 256;
    private const double TickIntervalMs = 50.0;
    private const double SpinThresholdMs = 16.0;

    private readonly Server _server;
    private readonly ConcurrentQueue<ISessionMessage> _inbox = new();
    private readonly ConcurrentDictionary<Orion.RakNet.NetworkConnection, PlayerSession> _sessions = new();

    private CancellationTokenSource? _runCancellation;
    private Thread? _workerThread;

    public int WorkerId { get; }

    internal int PendingMessageCount => _inbox.Count;

    internal int SessionCount => _sessions.Count;

    internal int WorkerThreadId { get; private set; }

    internal double LastTickWorkMs { get; private set; }

    public SessionWorker(int workerId, Server server)
    {
        WorkerId = workerId;
        _server = server;
    }

    public void Start()
    {
        if (_workerThread is not null)
        {
            return;
        }

        _runCancellation = new CancellationTokenSource();
        CancellationToken token = _runCancellation.Token;
        _workerThread = new Thread(() => WorkerLoop(token))
        {
            IsBackground = true,
            Name = $"session-worker-{WorkerId}"
        };
        _workerThread.Start();
    }

    public void Stop()
    {
        CancellationTokenSource? cancellation = _runCancellation;
        Thread? workerThread = _workerThread;
        _runCancellation = null;
        _workerThread = null;

        cancellation?.Cancel();
        try
        {
            workerThread?.Join(TimeSpan.FromSeconds(5));
        }
        finally
        {
            cancellation?.Dispose();
            DrainInbox(int.MaxValue);
            _sessions.Clear();
        }
    }

    public void Enqueue(ISessionMessage message) => _inbox.Enqueue(message);

    internal void RegisterSession(PlayerSession session) => _sessions[session.Connection] = session;

    internal void UnregisterSession(Orion.RakNet.NetworkConnection connection) => _sessions.TryRemove(connection, out _);

    internal bool IsCurrentThread() =>
        WorkerThreadId != 0 && Thread.CurrentThread.ManagedThreadId == WorkerThreadId;

    void WorkerLoop(CancellationToken token)
    {
        Thread.CurrentThread.Name = $"session-worker-{WorkerId}";
        WorkerThreadId = Thread.CurrentThread.ManagedThreadId;

        while (!token.IsCancellationRequested)
        {
            long tickStartTimestamp = Stopwatch.GetTimestamp();
            DrainInbox(MaxMessagesPerDrain);
            TickPlayerTraits();
            long tickEndTimestamp = Stopwatch.GetTimestamp();
            LastTickWorkMs = (tickEndTimestamp - tickStartTimestamp) * 1000.0 / Stopwatch.Frequency;
            SleepUntilDeadline(tickStartTimestamp + (long)(TickIntervalMs * Stopwatch.Frequency / 1000.0), token);
        }
    }

    void TickPlayerTraits()
    {
        foreach (PlayerSession session in _sessions.Values)
        {
            if (session.ActiveEntity is not Player.Player player)
            {
                continue;
            }

            if (session.TransferState == TransferState.Transferring)
            {
                continue;
            }

            // Iterate EntityTraitBase: plugin traits (e.g. EntityInventoryTrait) subclass
            // Orion.Api.EntityTraitBase, not Orion.Entity.Traits.EntityTrait.
            foreach (Orion.Api.Traits.EntityTraitBase trait in player.GetTraits())
            {
                if (trait is ISessionTickableTrait sessionTickable)
                {
                    sessionTickable.OnSessionTick();
                }
            }
        }
    }

    internal void DrainInbox(int maxMessages)
    {
        int processed = 0;
        while (processed < maxMessages && _inbox.TryDequeue(out ISessionMessage? message))
        {
            processed++;
            try
            {
                ProcessMessage(message);
            }
            catch (Exception exception)
            {
                Warn(
                    "Session worker {0} message error ({1}): {2}",
                    WorkerId,
                    message.GetType().Name,
                    exception);
            }
        }
    }

    void ProcessMessage(ISessionMessage message)
    {
        switch (message)
        {
            case SessionPacketMessage packetMessage:
                HandleSessionPacket(packetMessage);
                break;

            case ViewDeltaMessage viewDelta:
                SessionSendCoordinator.SendDirect(viewDelta.Session, viewDelta.Packet);
                break;

            case RunOnSessionThreadMessage runMessage:
                try
                {
                    runMessage.Action();
                    runMessage.Completion?.TrySetResult(null);
                }
                catch (Exception exception)
                {
                    runMessage.Completion?.TrySetException(exception);
                    throw;
                }

                break;
        }
    }

    void HandleSessionPacket(SessionPacketMessage message)
    {
        if (_server.Properties.AreaThreadingEnabled && _server.AreaScheduler.IsActive)
        {
            _server.AreaScheduler.EnqueueAreaPacket(message.Connection, message.PacketId, message.Payload);
            return;
        }

        _server.Scheduler.EnqueueGamePacket(message.Connection, message.PacketId, message.Payload);
    }

    static void SleepUntilDeadline(long deadlineTimestamp, CancellationToken token = default)
    {
        double remainingMs = (deadlineTimestamp - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency;
        if (remainingMs <= 0)
        {
            return;
        }

        while (remainingMs > SpinThresholdMs)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            Thread.Sleep(1);
            remainingMs = (deadlineTimestamp - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency;
            if (remainingMs <= 0)
            {
                return;
            }
        }

        while (Stopwatch.GetTimestamp() < deadlineTimestamp)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            Thread.SpinWait(1);
        }
    }
}
