using System.Collections.Concurrent;
using Orion.Config;
using Log = Orion.Logger.Logger;
using Orion.Player;
using Orion.Scheduling.Messages;
using Orion.World;
using Orion.World.Threading;

namespace Orion.Scheduling;

internal static class CrossAreaTransferHandler
{
    internal static readonly ConcurrentDictionary<object, byte> InFlightMobTransfers = new();

    /// <summary>
    /// RuntimeIds mid prepare→complete so spectators can keep the actor visible.
    /// </summary>
    internal static readonly ConcurrentDictionary<ulong, byte> InFlightTransferRuntimeIds = new();

    internal static bool IsTransferInFlight(ulong runtimeId) =>
        InFlightTransferRuntimeIds.ContainsKey(runtimeId);

    internal static void MarkTransferInFlight(ulong runtimeId) =>
        InFlightTransferRuntimeIds[runtimeId] = 1;

    internal static void ClearTransferInFlight(ulong runtimeId) =>
        InFlightTransferRuntimeIds.TryRemove(runtimeId, out _);

    public static void HandlePrepareTransfer(Server server, AreaWorker sourceWorker, PrepareAreaTransferMessage message)
    {
        AreaEntitySnapshot snapshot = message.Snapshot;
        object entity = snapshot.Entity;
        PlayerSession? session = snapshot.Session;

        if (GetEntityDimension(entity) is not Dimension dimension || !ReferenceEquals(dimension, snapshot.Dimension))
        {
            AbortTransfer(server, snapshot, "PrepareAreaTransfer: entity dimension mismatch.");
            return;
        }

        Thread prepareThread = Thread.CurrentThread;
        Log.Info(
            LogCategory.Orion,
            "[Area:Transfer] prepare {0} area={1}->{2} onAw={3} managedTid={4} workerTid={5} " +
            "threadName={6} onWorker={7} crossWorker={8}",
            DescribeEntity(entity),
            snapshot.SourceAreaIndex,
            snapshot.TargetAreaIndex,
            sourceWorker.WorkerId,
            prepareThread.ManagedThreadId,
            sourceWorker.WorkerThreadId,
            prepareThread.Name ?? "-",
            sourceWorker.IsCurrentThread(),
            snapshot.CrossWorker);

        dimension.ShardManager.GetShard(snapshot.SourceAreaIndex).RemoveEntity((IAreaStoredEntity)entity);
        sourceWorker.LogSimulationSnapshot("transfer-prepare");

        if (server.AreaScheduler is not AreaScheduler areaScheduler)
        {
            AbortTransfer(server, snapshot, "PrepareAreaTransfer: scheduler is not active.");
            return;
        }

        int? targetWorkerId = dimension.GetAreaShard(snapshot.TargetAreaIndex).AttachedWorkerId;
        if (!targetWorkerId.HasValue)
        {
            AbortTransfer(server, snapshot, "PrepareAreaTransfer: target area has no worker.");
            return;
        }

        AreaWorker targetWorker = areaScheduler.Pool.GetWorker(targetWorkerId.Value);
        CompleteAreaTransferMessage completeMessage = new() { Snapshot = snapshot };

        if (targetWorkerId == sourceWorker.WorkerId)
        {
            HandleCompleteTransfer(server, targetWorker, completeMessage);
            return;
        }

        targetWorker.Enqueue(completeMessage);
    }

    public static void HandleCompleteTransfer(Server server, AreaWorker targetWorker, CompleteAreaTransferMessage message)
    {
        AreaEntitySnapshot snapshot = message.Snapshot;
        object entity = snapshot.Entity;
        PlayerSession? session = snapshot.Session;
        Dimension dimension = snapshot.Dimension;

        try
        {
            Thread completeThread = Thread.CurrentThread;
            Log.Info(
                LogCategory.Orion,
                "[Area:Transfer] complete {0} area={1}->{2} onAw={3} managedTid={4} workerTid={5} " +
                "threadName={6} onWorker={7} crossWorker={8}",
                DescribeEntity(entity),
                snapshot.SourceAreaIndex,
                snapshot.TargetAreaIndex,
                targetWorker.WorkerId,
                completeThread.ManagedThreadId,
                targetWorker.WorkerThreadId,
                completeThread.Name ?? "-",
                targetWorker.IsCurrentThread(),
                snapshot.CrossWorker);

            dimension.AddEntity((IAreaStoredEntity)entity, snapshot.TargetAreaIndex);
            ClearTransferInFlight(GetRuntimeId(entity));
            targetWorker.LogSimulationSnapshot("transfer-complete");
            if (session is not null)
            {
                session.ActiveEntity = entity as Orion.Player.Player;
                session.TransferState = TransferState.Idle;
                session.PendingTransferAreaIndex = null;

                if (entity is Orion.Player.Player transferredPlayer)
                {
                    bool crossWorker = snapshot.CrossWorker;
                    Log.Info(
                        LogCategory.Orion,
                        "[Teleport:Area] ownership player={0} area={1}->{2} owningArea={3} " +
                        "simAw={4} simTid={5} onWorker={6} pos=({7:0.##},{8:0.##},{9:0.##})",
                        transferredPlayer.Username,
                        snapshot.SourceAreaIndex,
                        snapshot.TargetAreaIndex,
                        transferredPlayer.OwningAreaIndex?.ToString() ?? "-",
                        targetWorker.WorkerId,
                        completeThread.ManagedThreadId,
                        targetWorker.IsCurrentThread(),
                        transferredPlayer.Position.X,
                        transferredPlayer.Position.Y,
                        transferredPlayer.Position.Z);

                    if (server.ConnectionCoordinator is ConnectionCoordinator coordinator && coordinator.IsActive)
                    {
                        coordinator.RunOnSessionThread(session, () => transferredPlayer.ResyncAfterRegionHandoff(crossWorker));
                    }
                    else
                    {
                        transferredPlayer.ResyncAfterRegionHandoff(crossWorker);
                    }
                }
            }
            else
            {
                InFlightMobTransfers.TryRemove(entity, out _);
            }
        }
        catch (Exception exception)
        {
            AbortTransfer(server, snapshot, $"CompleteAreaTransfer failed: {exception.Message}");
        }
    }

    public static void HandleAbortTransfer(Server server, AbortAreaTransferMessage message) =>
        AbortTransfer(server, message.Snapshot, message.Reason);

    static void AbortTransfer(Server server, AreaEntitySnapshot snapshot, string reason)
    {
        Log.Error(LogCategory.Orion, "[Area:Transfer] abort: {0}", reason);
        InFlightMobTransfers.TryRemove(snapshot.Entity, out _);
        ClearTransferInFlight(GetRuntimeId(snapshot.Entity));

        if (snapshot.Session is not null)
        {
            snapshot.Session.TransferState = TransferState.Idle;
            snapshot.Session.PendingTransferAreaIndex = null;
            snapshot.Session.SendMessage("§cArea transfer failed. Please reconnect.");
            snapshot.Session.Disconnect("Area transfer failed.");
        }
    }

    static Dimension? GetEntityDimension(object entity) =>
        entity is IAreaEntity areaEntity ? areaEntity.Dimension : null;

    static ulong GetRuntimeId(object entity) =>
        entity is IAreaEntity areaEntity ? areaEntity.RuntimeId : 0UL;

    static string DescribeEntity(object entity) =>
        entity is Orion.Player.Player player
            ? $"player={player.Username}"
            : entity is IAreaEntity areaEntity
                ? $"entity runtime={areaEntity.RuntimeId}"
                : $"entity={entity.GetType().Name}";
}
