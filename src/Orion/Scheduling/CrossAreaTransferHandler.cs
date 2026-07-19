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

        dimension.ShardManager.GetShard(snapshot.SourceAreaIndex).RemoveEntity((IAreaStoredEntity)entity);

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
            dimension.AddEntity((IAreaStoredEntity)entity, snapshot.TargetAreaIndex);
            ClearTransferInFlight(GetRuntimeId(entity));
            if (session is not null)
            {
                session.ActiveEntity = entity as Orion.Player.Player;
                session.TransferState = TransferState.Idle;
                session.PendingTransferAreaIndex = null;

                if (entity is Orion.Player.Player transferredPlayer)
                {
                    if (server.ConnectionCoordinator is ConnectionCoordinator coordinator && coordinator.IsActive)
                    {
                        coordinator.RunOnSessionThread(session, transferredPlayer.ResyncAfterRegionHandoff);
                    }
                    else
                    {
                        transferredPlayer.ResyncAfterRegionHandoff();
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
}
