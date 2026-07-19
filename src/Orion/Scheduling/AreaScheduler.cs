using Orion.Logger;
using Orion.Player;
using Orion.Protocol.Enums;
using Orion.Protocol.Types;
using Orion.RakNet;
using Orion.Scheduling.Messages;
using Orion.World;
using Orion.World.Coordinates;
using Orion.World.Threading;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.Scheduling;

public sealed class AreaScheduler : IAreaScheduler
{
    private const double ScoreEpsilon = 0.01;

    private readonly Server _server;
    private readonly AreaWorkerPool _pool;

    public AreaScheduler(Server server)
    {
        _server = server;
        _pool = new AreaWorkerPool(server.Properties.AreaThreadCount, server);
    }

    public bool IsActive => true;

    internal AreaWorkerPool Pool => _pool;

    public void Start() => _pool.Start();

    public void Stop() => _pool.Stop();

    public void RequestAttachArea(Dimension dimension, int areaIndex)
    {
        AreaShard area = dimension.GetAreaShard(areaIndex);
        if (area.IsAttached)
        {
            return;
        }

        int workerId = ResolveWorkerForArea(areaIndex);
        area.AttachedWorkerId = workerId;
        _pool.GetWorker(workerId).Enqueue(new AttachAreaMessage
        {
            Dimension = dimension,
            AreaIndex = areaIndex
        });
    }

    public void RequestDetachArea(Dimension dimension, int areaIndex)
    {
        AreaShard area = dimension.GetAreaShard(areaIndex);
        if (area.PresenceCount > 0 || !area.AttachedWorkerId.HasValue)
        {
            return;
        }

        int workerId = area.AttachedWorkerId.Value;
        _pool.GetWorker(workerId).Enqueue(new DetachAreaMessage
        {
            Dimension = dimension,
            AreaIndex = areaIndex
        });
    }

    public IReadOnlyList<AreaWorkerLoadMetrics> GetMetrics() => _pool.GetAllMetrics();

    public int? GetAttachedWorkerId(Dimension dimension, int areaIndex) =>
        dimension.GetAreaShard(areaIndex).AttachedWorkerId;

    public void EnqueueAreaPacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> payload)
    {
        AreaRouteTarget? target = ResolveRouteTarget(connection, packetId);
        if (target is not AreaRouteTarget routeTarget)
        {
            return;
        }

        _pool.GetWorker(routeTarget.WorkerId).Enqueue(new ProcessAreaPacketMessage
        {
            Connection = connection,
            PacketId = packetId,
            Payload = payload.ToArray(),
            Dimension = routeTarget.Dimension,
            AreaIndex = routeTarget.AreaIndex
        });
    }

    public void EnqueueAreaDisconnect(NetworkConnection connection)
    {
        int? workerId = ResolveRouteTarget(connection, packetId: null)?.WorkerId;
        if (!workerId.HasValue)
        {
            _server.Network.ProcessDisconnectOnWorker(connection);
            return;
        }

        _pool.GetWorker(workerId.Value).Enqueue(new ProcessAreaDisconnectMessage
        {
            Connection = connection
        });
    }

    public void RunOnAreaThread(AreaHandle area, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        EnsureAreaAttached(area.Dimension, area.AreaIndex);
        int workerId = area.Area.AttachedWorkerId
            ?? throw new InvalidOperationException($"Area {area.AreaIndex} is not attached.");

        AreaWorker worker = _pool.GetWorker(workerId);
        if (worker.IsCurrentThread())
        {
#if DEBUG
            ThreadGuard.CurrentAreaIndex = area.AreaIndex;
#endif
            action();
            return;
        }

        TaskCompletionSource<object?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Enqueue(new RunOnAreaThreadMessage
        {
            Action = () =>
            {
#if DEBUG
                ThreadGuard.CurrentAreaIndex = area.AreaIndex;
#endif
                action();
            },
            Completion = completion
        });
        completion.Task.GetAwaiter().GetResult();
    }

    public void EnqueueCompletedChunk(Dimension dimension, int areaIndex, long hash, ChunkColumn? chunk)
    {
        EnsureAreaAttached(dimension, areaIndex);
        int? workerId = dimension.GetAreaShard(areaIndex).AttachedWorkerId;
        if (!workerId.HasValue)
        {
            return;
        }

        _pool.GetWorker(workerId.Value).Enqueue(new CompletedChunkMessage
        {
            Dimension = dimension,
            AreaIndex = areaIndex,
            Hash = hash,
            Chunk = chunk
        });
    }

    public void BeginAreaTransfer(PlayerSession? session, AreaEntitySnapshot snapshot)
    {
        Dimension dimension = snapshot.Dimension;
        EnsureAreaAttached(dimension, snapshot.SourceAreaIndex);
        EnsureAreaAttached(dimension, snapshot.TargetAreaIndex);

        AreaShard sourceArea = dimension.GetAreaShard(snapshot.SourceAreaIndex);
        int? sourceWorkerId = sourceArea.AttachedWorkerId;
        int? targetWorkerId = dimension.GetAreaShard(snapshot.TargetAreaIndex).AttachedWorkerId;
        if (!sourceWorkerId.HasValue || !targetWorkerId.HasValue)
        {
            CrossAreaTransferHandler.InFlightMobTransfers.TryRemove(GetEntityKey(snapshot.Entity), out _);
            if (snapshot.Entity is IAreaEntity areaEntity)
            {
                CrossAreaTransferHandler.ClearTransferInFlight(areaEntity.RuntimeId);
            }

            return;
        }

        if (session is not null)
        {
            if (session.TransferState != TransferState.Idle)
            {
                return;
            }

            session.TransferState = TransferState.Transferring;
            session.PendingTransferAreaIndex = snapshot.TargetAreaIndex;
        }

        PrepareAreaTransferMessage prepareMessage = new() { Snapshot = snapshot };
        AreaWorker sourceWorker = _pool.GetWorker(sourceWorkerId.Value);

        if (sourceWorker.IsCurrentThread())
        {
            CrossAreaTransferHandler.HandlePrepareTransfer(_server, sourceWorker, prepareMessage);
            return;
        }

        sourceWorker.Enqueue(prepareMessage);
    }

    internal int ResolveWorkerForArea(int areaIndex)
    {
        if ((uint)areaIndex < (uint)_pool.WorkerCount)
        {
            return areaIndex;
        }

        return PickAreaWorker();
    }

    internal int PickAreaWorker()
    {
        int bestWorker = 0;
        double bestScore = double.MaxValue;
        int bestAreaCount = int.MaxValue;

        for (int workerId = 0; workerId < _pool.WorkerCount; workerId++)
        {
            AreaWorker worker = _pool.GetWorker(workerId);
            AreaWorkerLoadMetrics metrics = GetWorkerLoad(worker);
            double score = ComputeScore(metrics);
            if (score + ScoreEpsilon < bestScore
                || (Math.Abs(score - bestScore) <= ScoreEpsilon && metrics.ActiveAreaCount < bestAreaCount))
            {
                bestScore = score;
                bestWorker = workerId;
                bestAreaCount = metrics.ActiveAreaCount;
            }
        }

        return bestWorker;
    }

    AreaWorkerLoadMetrics GetWorkerLoad(AreaWorker worker)
    {
        worker.RefreshMetrics();
        return worker.Metrics;
    }

    internal static double ComputeScore(AreaWorkerLoadMetrics metrics) =>
        metrics.ActiveAreaCount
        + metrics.TotalPresenceCount * 0.5
        + metrics.LastTickWorkMs
        + metrics.TickLagMs * 2.0;

    internal AreaRouteTarget? ResolveRouteTarget(NetworkConnection connection, PacketId? packetId)
    {
        if (packetId == PacketId.ResourcePackClientResponse)
        {
            Dimension dimension = _server.GetWorld().GetDimension(DimensionType.Overworld)
                ?? throw new InvalidOperationException("Default overworld dimension is missing.");
            int spawnAreaIndex = ResolveSpawnAreaIndex(dimension, connection);
            EnsureAreaAttached(dimension, spawnAreaIndex);
            int? workerId = dimension.GetAreaShard(spawnAreaIndex).AttachedWorkerId;
            if (!workerId.HasValue)
            {
                return null;
            }

            return new AreaRouteTarget(workerId.Value, dimension, spawnAreaIndex);
        }

        if (!_server.Sessions.TryGetValue(connection, out PlayerSession? session))
        {
            return null;
        }

        if (session.TransferState == TransferState.Transferring)
        {
            if (session.PendingTransferAreaIndex is not int targetAreaIndex
                || session.ActiveEntity is not { } transferEntity
                || GetEntityDimension(transferEntity) is not Dimension transferDimension
                || !transferDimension.UsesAreaThreading())
            {
                return null;
            }

            EnsureAreaAttached(transferDimension, targetAreaIndex);
            int? transferWorkerId = transferDimension.GetAreaShard(targetAreaIndex).AttachedWorkerId;
            if (!transferWorkerId.HasValue)
            {
                return null;
            }

            return new AreaRouteTarget(transferWorkerId.Value, transferDimension, targetAreaIndex);
        }

        if (session.ActiveEntity is not { } activeEntity
            || GetEntityDimension(activeEntity) is not Dimension dimensionWithArea
            || !dimensionWithArea.UsesAreaThreading())
        {
            return null;
        }

        int areaIndex = GetEntityAreaIndex(activeEntity, dimensionWithArea);
        EnsureAreaAttached(dimensionWithArea, areaIndex);
        int? attachedWorkerId = dimensionWithArea.GetAreaShard(areaIndex).AttachedWorkerId;
        if (!attachedWorkerId.HasValue)
        {
            return null;
        }

        return new AreaRouteTarget(attachedWorkerId.Value, dimensionWithArea, areaIndex);
    }

    internal readonly record struct AreaRouteTarget(int WorkerId, Dimension Dimension, int AreaIndex);

    int ResolveSpawnAreaIndex(Dimension dimension, NetworkConnection connection)
    {
        if (_server.Sessions.TryGetValue(connection, out PlayerSession? session)
            && session.ActiveEntity is { } entity)
        {
            return GetEntityAreaIndex(entity, dimension);
        }

        return AreaResolver.DefaultThread;
    }

    void EnsureAreaAttached(Dimension dimension, int areaIndex)
    {
        AreaShard area = dimension.GetAreaShard(areaIndex);
        if (area.IsAttached)
        {
            return;
        }

        RequestAttachArea(dimension, areaIndex);
    }

    internal static object GetEntityKey(object entity) => entity;

    internal static Dimension? GetEntityDimension(object entity) =>
        entity is IAreaEntity areaEntity ? areaEntity.Dimension : null;

    internal static int GetEntityAreaIndex(object entity, Dimension dimension)
    {
        if (entity is IAreaEntity areaEntity && areaEntity.OwningAreaIndex.HasValue)
        {
            return areaEntity.OwningAreaIndex.Value;
        }

        Vec3f position = entity is IAreaEntity positioned ? positioned.Position : default;
        return dimension.ResolveAreaIndex(position.X, position.Z);
    }
}

public interface IAreaEntity
{
    Dimension? Dimension { get; }

    int? OwningAreaIndex { get; }

    Vec3f Position { get; }

    ulong RuntimeId { get; }
}
