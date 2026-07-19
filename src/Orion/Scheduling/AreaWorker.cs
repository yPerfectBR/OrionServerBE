using System.Collections.Concurrent;
using System.Diagnostics;
using Orion.Config;
using Log = Orion.Logger.Logger;
using Orion.Scheduling.Messages;
using Orion.World;
using Orion.World.Threading;
using WorldInstance = Orion.World.World;
using GameEntity = Orion.Entity.Entity;

namespace Orion.Scheduling;

public sealed class AreaWorker
{
    private const int MaxMessagesPerDrain = 256;
    private const double TickIntervalMs = 50.0;
    private const double SpinThresholdMs = 16.0;
    private const ulong TpsUpdateIntervalTicks = 20;

    private readonly Server _server;
    private readonly ConcurrentQueue<IAreaMessage> _inbox = new();
    private readonly Dictionary<AttachedAreaKey, AreaShard> _attachedAreas = [];

    private CancellationTokenSource? _runCancellation;
    private Thread? _workerThread;
    private long _lastTpsTimestamp;
    private ulong _lastTpsTick;
    private ulong _tickValue;

    public int WorkerId { get; }

    public AreaWorkerLoadMetrics Metrics { get; } = new();

    internal int PendingMessageCount => _inbox.Count;

    internal int WorkerThreadId { get; private set; }

    internal int LastActionThreadId { get; private set; }

    public AreaWorker(int workerId, Server server)
    {
        WorkerId = workerId;
        _server = server;
        Metrics.WorkerId = workerId;
    }

    internal void RefreshMetrics()
    {
        Metrics.ActiveAreaCount = _attachedAreas.Count;
        Metrics.TotalPresenceCount = 0;
        foreach (AreaShard area in _attachedAreas.Values)
        {
            Metrics.TotalPresenceCount += area.PresenceCount;
        }
    }

    internal bool HasAttachedArea(Dimension dimension, int areaIndex) =>
        _attachedAreas.ContainsKey(new AttachedAreaKey(dimension, areaIndex));

    public void Start()
    {
        if (_workerThread is not null)
        {
            return;
        }

        _runCancellation = new CancellationTokenSource();
        CancellationToken token = _runCancellation.Token;
        _lastTpsTimestamp = Stopwatch.GetTimestamp();
        _lastTpsTick = 0;
        _workerThread = new Thread(() => WorkerLoop(token))
        {
            IsBackground = true,
            Name = $"area-worker-{WorkerId}"
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
        }
    }

    public void Enqueue(IAreaMessage message) => _inbox.Enqueue(message);

    internal bool IsCurrentThread()
    {
#if DEBUG
        if (ThreadGuard.CurrentAreaWorkerId == WorkerId)
        {
            return true;
        }
#endif

        return WorkerThreadId != 0
            && Thread.CurrentThread.ManagedThreadId == WorkerThreadId;
    }

    void WorkerLoop(CancellationToken token)
    {
        Thread.CurrentThread.Name = $"area-worker-{WorkerId}";
        WorkerThreadId = Thread.CurrentThread.ManagedThreadId;

        while (!token.IsCancellationRequested)
        {
            long tickStartTimestamp = Stopwatch.GetTimestamp();
#if DEBUG
            ThreadGuard.CurrentAreaWorkerId = WorkerId;
#endif

            DrainInbox(MaxMessagesPerDrain);
            TickAttachedAreas();

            long tickEndTimestamp = Stopwatch.GetTimestamp();
            Metrics.LastTickWorkMs = (tickEndTimestamp - tickStartTimestamp) * 1000.0 / Stopwatch.Frequency;
            UpdateMetrics(tickEndTimestamp);

            long tickDeadlineTimestamp = tickStartTimestamp + (long)(TickIntervalMs * Stopwatch.Frequency / 1000.0);
            SleepUntilDeadline(tickDeadlineTimestamp, token);
        }
    }

    void TickAttachedAreas()
    {
        _tickValue++;

        WorldInstance? world = _server.World as WorldInstance;
        if (WorkerId == 0 && world is not null)
        {
            world.AttachedWorkerId = WorkerId;
            Stopwatch worldWork = Stopwatch.StartNew();
            world.Tick();
            world.TickWork = worldWork.Elapsed.TotalMilliseconds;
        }

        TickAttachedEntities(world);
        SaveAttachedDirtyChunks();
        MaybeLogSimulationHeartbeat();
    }

    void MaybeLogSimulationHeartbeat()
    {
        // ~5s at 20 TPS — quiet periodic ownership proof.
        if (_tickValue % 100 != 0 || _attachedAreas.Count == 0)
        {
            return;
        }

        LogSimulationSnapshot("heartbeat");
    }

    /// <summary>
    /// Logs which entities/players this worker is currently simulating.
    /// Used for the periodic heartbeat and immediately on important ownership changes.
    /// </summary>
    internal void LogSimulationSnapshot(string reason)
    {
        int entityCount = 0;
        List<string> playerSummaries = [];
        foreach ((AttachedAreaKey key, AreaShard shard) in _attachedAreas)
        {
            IAreaStoredEntity[] entities = shard.SnapshotEntities();
            entityCount += entities.Length;
            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] is Orion.Player.Player player)
                {
                    playerSummaries.Add(
                        $"{player.Username}@area{key.AreaIndex}(own={player.OwningAreaIndex?.ToString() ?? "-"})");
                }
            }
        }

        if (entityCount == 0 && reason == "heartbeat")
        {
            return;
        }

        Thread thread = Thread.CurrentThread;
        Log.Info(
            LogCategory.Orion,
            "[Area:Sim] reason={0} aw{1} managedTid={2} workerTid={3} threadName={4} onWorker={5} tick={6} " +
            "areas={7} entities={8} players=[{9}]",
            reason,
            WorkerId,
            thread.ManagedThreadId,
            WorkerThreadId,
            thread.Name ?? "-",
            IsCurrentThread(),
            _tickValue,
            _attachedAreas.Count,
            entityCount,
            playerSummaries.Count == 0 ? "-" : string.Join(", ", playerSummaries));
    }

    void SaveAttachedDirtyChunks()
    {
        if (_tickValue % 20 != 0 || _attachedAreas.Count == 0)
        {
            return;
        }

        foreach ((AttachedAreaKey key, AreaShard shard) in _attachedAreas)
        {
            key.Dimension.SaveDirtyChunks(shard);
        }
    }

    void TickAttachedEntities(WorldInstance? world)
    {
        if (_attachedAreas.Count == 0)
        {
            return;
        }

        ulong currentTick = world?.TickValue ?? _tickValue;
        List<GameEntity> pendingRemoves = [];

#if DEBUG
        int? previousAreaIndex = ThreadGuard.CurrentAreaIndex;
#endif
        try
        {
            foreach ((AttachedAreaKey key, AreaShard shard) in _attachedAreas)
            {
#if DEBUG
                ThreadGuard.CurrentAreaIndex = key.AreaIndex;
#endif
                // Snapshot: entity.Tick may despawn/merge and mutate the shard set.
                IAreaStoredEntity[] entities = shard.SnapshotEntities();
                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i] is not GameEntity entity)
                    {
                        continue;
                    }

                    if (entity.PendingDespawn || entity.Dimension is null)
                    {
                        pendingRemoves.Add(entity);
                        continue;
                    }

                    entity.Tick(currentTick, 1);

                    if (entity.PendingDespawn)
                    {
                        pendingRemoves.Add(entity);
                    }
                }
            }
        }
        finally
        {
#if DEBUG
            ThreadGuard.CurrentAreaIndex = previousAreaIndex;
#endif
        }

        for (int i = 0; i < pendingRemoves.Count; i++)
        {
            GameEntity entity = pendingRemoves[i];
            if (entity.Dimension is Dimension dimension)
            {
                dimension.RemoveEntity(entity);
                continue;
            }

            foreach (AreaShard shard in _attachedAreas.Values)
            {
                shard.RemoveEntity(entity);
            }

            entity.CompleteDespawn();
        }
    }

    void UpdateMetrics(long timestamp)
    {
        RefreshMetrics();

        if (_lastTpsTimestamp == 0)
        {
            _lastTpsTimestamp = timestamp;
            _lastTpsTick = _tickValue;
            return;
        }

        ulong tickDelta = _tickValue - _lastTpsTick;
        if (tickDelta < TpsUpdateIntervalTicks)
        {
            return;
        }

        long timestampDelta = timestamp - _lastTpsTimestamp;
        if (timestampDelta <= 0)
        {
            return;
        }

        double elapsedSeconds = (double)timestampDelta / Stopwatch.Frequency;
        double currentTps = Math.Min(20.0, tickDelta / elapsedSeconds);
        Metrics.Tps = Metrics.Tps == 0 ? currentTps : Metrics.Tps + ((currentTps - Metrics.Tps) * 0.2);
        Metrics.TickLagMs = Math.Max(0, Metrics.LastTickWorkMs - TickIntervalMs);
        if (WorkerId == 0)
        {
            _server.SetTps(Metrics.Tps);
        }
        _lastTpsTimestamp = timestamp;
        _lastTpsTick = _tickValue;
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

    internal void DrainInbox(int maxMessages)
    {
#if DEBUG
        int? previousWorkerId = ThreadGuard.CurrentAreaWorkerId;
        ThreadGuard.CurrentAreaWorkerId = WorkerId;
        try
        {
            DrainInboxCore(maxMessages);
        }
        finally
        {
            ThreadGuard.CurrentAreaWorkerId = previousWorkerId;
        }
#else
        DrainInboxCore(maxMessages);
#endif
    }

    void DrainInboxCore(int maxMessages)
    {
        List<IAreaMessage> batch = [];
        while (batch.Count < maxMessages && _inbox.TryDequeue(out IAreaMessage? message))
        {
            batch.Add(message);
        }

        batch.Sort(static (a, b) => GetMessagePriority(a).CompareTo(GetMessagePriority(b)));

        foreach (IAreaMessage message in batch)
        {
            try
            {
                ProcessMessage(message);
            }
            catch (Exception exception)
            {
                Log.Warn(
                    LogCategory.Orion,
                    "Area worker {0} message error ({1}): {2}",
                    WorkerId,
                    message.GetType().Name,
                    exception);
            }
        }
    }

    static int GetMessagePriority(IAreaMessage message) =>
        message switch
        {
            DetachAreaMessage => 0,
            AttachAreaMessage => 1,
            CompleteAreaTransferMessage => 2,
            PrepareAreaTransferMessage => 3,
            AbortAreaTransferMessage => 4,
            RunOnAreaThreadMessage => 5,
            CompletedChunkMessage => 6,
            PluginResultMessage => 7,
            ProcessAreaDisconnectMessage => 8,
            ProcessAreaPacketMessage => 9,
            _ => 10
        };

    void ProcessMessage(IAreaMessage message)
    {
        switch (message)
        {
            case AttachAreaMessage attachMessage:
                HandleAttach(attachMessage);
                break;

            case DetachAreaMessage detachMessage:
                HandleDetach(detachMessage);
                break;

            case ProcessAreaPacketMessage packetMessage:
                HandleProcessPacket(packetMessage);
                break;

            case ProcessAreaDisconnectMessage disconnectMessage:
                _server.Network.ProcessDisconnectOnWorker(disconnectMessage.Connection);
                break;

            case RunOnAreaThreadMessage runMessage:
                HandleRunOnAreaThread(runMessage);
                break;

            case PrepareAreaTransferMessage prepareMessage:
                CrossAreaTransferHandler.HandlePrepareTransfer(_server, this, prepareMessage);
                break;

            case CompleteAreaTransferMessage completeMessage:
                CrossAreaTransferHandler.HandleCompleteTransfer(_server, this, completeMessage);
                break;

            case AbortAreaTransferMessage abortMessage:
                CrossAreaTransferHandler.HandleAbortTransfer(_server, abortMessage);
                break;

            case CompletedChunkMessage completedChunkMessage:
                HandleCompletedChunk(completedChunkMessage);
                break;

            case PluginResultMessage pluginResultMessage:
                HandlePluginResult(pluginResultMessage);
                break;
        }
    }

    void HandleCompletedChunk(CompletedChunkMessage message)
    {
        LastActionThreadId = Thread.CurrentThread.ManagedThreadId;
#if DEBUG
        AreaShard area = message.Dimension.GetAreaShard(message.AreaIndex);
        System.Diagnostics.Debug.Assert(area.AttachedWorkerId == WorkerId);

        int? previousAreaIndex = ThreadGuard.CurrentAreaIndex;
        ThreadGuard.CurrentAreaIndex = message.AreaIndex;
        try
        {
            message.Dimension.CommitCompletedChunk(message.Hash, message.Chunk);
        }
        finally
        {
            ThreadGuard.CurrentAreaIndex = previousAreaIndex;
        }
#else
        message.Dimension.CommitCompletedChunk(message.Hash, message.Chunk);
#endif
    }

    void HandlePluginResult(PluginResultMessage message)
    {
        LastActionThreadId = Thread.CurrentThread.ManagedThreadId;
#if DEBUG
        System.Diagnostics.Debug.Assert(ThreadGuard.CurrentAreaWorkerId == WorkerId);
#endif
        try
        {
            message.Apply();
            message.Completion?.TrySetResult(null);
        }
        catch (Exception exception)
        {
            message.Completion?.TrySetException(exception);
            throw;
        }
    }

    void HandleRunOnAreaThread(RunOnAreaThreadMessage message)
    {
        LastActionThreadId = Thread.CurrentThread.ManagedThreadId;
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

    void HandleAttach(AttachAreaMessage message)
    {
        AttachedAreaKey key = new(message.Dimension, message.AreaIndex);
        if (_attachedAreas.ContainsKey(key))
        {
            return;
        }

        AreaShard area = message.Dimension.GetAreaShard(message.AreaIndex);
        area.AttachedWorkerId = WorkerId;
        _attachedAreas[key] = area;
        RefreshMetrics();

        if (_server.Properties.AreaSchedulerDebug)
        {
            Log.Debug(
                LogCategory.Orion,
                "[Area:Attach] worker={0} dimension={1} area={2}",
                WorkerId,
                message.Dimension.Identifier,
                message.AreaIndex);
        }
    }

    void HandleDetach(DetachAreaMessage message)
    {
        AttachedAreaKey key = new(message.Dimension, message.AreaIndex);
        if (!_attachedAreas.TryGetValue(key, out AreaShard? area))
        {
            return;
        }

        if (area.PresenceCount > 0)
        {
            Log.Warn(
                LogCategory.Orion,
                "Area detach skipped for index {0}: PresenceCount={1}",
                message.AreaIndex,
                area.PresenceCount);
            return;
        }

        _attachedAreas.Remove(key);
        area.AttachedWorkerId = null;
        RefreshMetrics();

        if (_server.Properties.AreaSchedulerDebug)
        {
            Log.Debug(
                LogCategory.Orion,
                "[Area:Detach] worker={0} dimension={1} area={2}",
                WorkerId,
                message.Dimension.Identifier,
                message.AreaIndex);
        }
    }

    void HandleProcessPacket(ProcessAreaPacketMessage packetMessage)
    {
        int areaIndex = packetMessage.AreaIndex;

        if (_server.AreaScheduler is AreaScheduler scheduler)
        {
            AreaScheduler.AreaRouteTarget? liveRoute =
                scheduler.ResolveRouteTarget(packetMessage.Connection, packetMessage.PacketId);
            if (liveRoute is null)
            {
                return;
            }

            if (liveRoute.Value.WorkerId != WorkerId
                || liveRoute.Value.AreaIndex != packetMessage.AreaIndex)
            {
                scheduler.Pool.GetWorker(liveRoute.Value.WorkerId).Enqueue(new ProcessAreaPacketMessage
                {
                    Connection = packetMessage.Connection,
                    PacketId = packetMessage.PacketId,
                    Payload = packetMessage.Payload,
                    Dimension = liveRoute.Value.Dimension,
                    AreaIndex = liveRoute.Value.AreaIndex
                });
                return;
            }

            areaIndex = liveRoute.Value.AreaIndex;
        }

#if DEBUG
        ThreadGuard.CurrentAreaIndex = areaIndex;
#endif

        try
        {
            _server.Network.HandleGamePacketOnWorker(
                packetMessage.Connection,
                packetMessage.PacketId,
                packetMessage.Payload);
        }
        finally
        {
#if DEBUG
            ThreadGuard.CurrentAreaIndex = null;
#endif
        }
    }
}
