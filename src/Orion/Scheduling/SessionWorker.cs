using System.Collections.Concurrent;
using System.Diagnostics;
using Orion.Network;
using Orion.Player;
using Orion.Scheduling.Messages;

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
    private Task? _loopTask;

    public int WorkerId { get; }

    internal int PendingMessageCount => _inbox.Count;

    internal int SessionCount => _sessions.Count;

    internal int WorkerThreadId { get; private set; }

    public SessionWorker(int workerId, Server server)
    {
        WorkerId = workerId;
        _server = server;
    }

    public void Start()
    {
        if (_loopTask is not null)
        {
            return;
        }

        _runCancellation = new CancellationTokenSource();
        CancellationToken token = _runCancellation.Token;
        _loopTask = Task.Run(() => WorkerLoop(token), token);
    }

    public void Stop()
    {
        CancellationTokenSource? cancellation = _runCancellation;
        Task? loopTask = _loopTask;
        _runCancellation = null;
        _loopTask = null;

        cancellation?.Cancel();
        try
        {
            loopTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(static inner => inner is TaskCanceledException))
        { }
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
            SleepUntilDeadline(tickStartTimestamp + (long)(TickIntervalMs * Stopwatch.Frequency / 1000.0));
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
                Warn("Session worker {0} message error: {1}", WorkerId, exception.Message);
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

    static void SleepUntilDeadline(long deadlineTimestamp)
    {
        double remainingMs = (deadlineTimestamp - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency;
        if (remainingMs <= 0)
        {
            return;
        }

        while (remainingMs > SpinThresholdMs)
        {
            Thread.Sleep(1);
            remainingMs = (deadlineTimestamp - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency;
            if (remainingMs <= 0)
            {
                return;
            }
        }

        while (Stopwatch.GetTimestamp() < deadlineTimestamp)
        {
            Thread.SpinWait(1);
        }
    }
}
