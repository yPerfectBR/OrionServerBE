using System.Collections.Concurrent;
using Orion.Config;
using Log = Orion.Logger.Logger;
using Orion.Player;
using Orion.Protocol.Types;
using Orion.World;
using Orion.World.Threading;

namespace Orion.Scheduling;

public static class AreaBorderTransfer
{
    private const ulong TransferCooldownTicks = 10;

    private static readonly ConcurrentDictionary<ulong, ulong> LastTransferTickByRuntimeId = new();

    public static void ResetTransferCooldown(ulong runtimeId) =>
        LastTransferTickByRuntimeId.TryRemove(runtimeId, out _);

    public static bool TryAfterMove(Server server, IAreaEntity entity, Vec3f previousPosition)
    {
        if (!server.Properties.AreaThreadingEnabled || !server.AreaScheduler.IsActive)
        {
            return false;
        }

        if (entity.Dimension is not Dimension dimension || !dimension.UsesAreaThreading())
        {
            return false;
        }

        int sourceAreaIndex = entity.OwningAreaIndex
            ?? dimension.ResolveAreaIndex(previousPosition.X, previousPosition.Z);
        int targetAreaIndex = dimension.ResolveAreaIndex(entity.Position.X, entity.Position.Z);
        if (sourceAreaIndex == targetAreaIndex)
        {
            return false;
        }

        ulong currentTick = dimension.World?.TickValue ?? 0;
        if (LastTransferTickByRuntimeId.TryGetValue(entity.RuntimeId, out ulong lastTransferTick)
            && currentTick - lastTransferTick < TransferCooldownTicks)
        {
            return false;
        }

        PlayerSession? session = entity is IPlayerWithSession player ? player.Session : null;
        if (session is not null && session.TransferState != TransferState.Idle)
        {
            return true;
        }

        if (session is null && CrossAreaTransferHandler.InFlightMobTransfers.ContainsKey(entity))
        {
            return true;
        }

        BeginTransfer(server, dimension, entity, session, sourceAreaIndex, targetAreaIndex, entity.Position);
        LastTransferTickByRuntimeId[entity.RuntimeId] = currentTick;
        return true;
    }

    /// <summary>
    /// Starts an area transfer after the entity position has already been updated
    /// to the teleport destination (unlike <see cref="TryAfterMove"/>, skips the border cooldown).
    /// </summary>
    public static bool TryAfterTeleport(Server server, IAreaEntity player, Vec3f previousPosition)
    {
        if (!server.Properties.AreaThreadingEnabled || !server.AreaScheduler.IsActive)
        {
            return false;
        }

        if (player.Dimension is not Dimension dimension || !dimension.UsesAreaThreading())
        {
            return false;
        }

        PlayerSession? session = player is IPlayerWithSession playerWithSession ? playerWithSession.Session : null;
        if (session is not null && session.TransferState != TransferState.Idle)
        {
            return true;
        }

        int sourceAreaIndex = player.OwningAreaIndex
            ?? dimension.ResolveAreaIndex(previousPosition.X, previousPosition.Z);
        int targetAreaIndex = dimension.ResolveAreaIndex(player.Position.X, player.Position.Z);
        if (sourceAreaIndex == targetAreaIndex)
        {
            return false;
        }

        LastTransferTickByRuntimeId.TryRemove(player.RuntimeId, out _);
        BeginTransfer(server, dimension, player, session, sourceAreaIndex, targetAreaIndex, player.Position);
        return true;
    }

    static void BeginTransfer(
        Server server,
        Dimension dimension,
        IAreaEntity entity,
        PlayerSession? session,
        int sourceAreaIndex,
        int targetAreaIndex,
        Vec3f position)
    {
        if (server.AreaScheduler is not AreaScheduler areaScheduler)
        {
            return;
        }

        EnsureAreasAttached(areaScheduler, dimension, sourceAreaIndex, targetAreaIndex);

        AreaShard sourceShard = dimension.GetAreaShard(sourceAreaIndex);
        AreaShard targetShard = dimension.GetAreaShard(targetAreaIndex);
        int? sourceWorkerId = sourceShard.AttachedWorkerId;
        int? targetWorkerId = targetShard.AttachedWorkerId;
        if (!sourceWorkerId.HasValue || !targetWorkerId.HasValue)
        {
            Log.Warn(
                LogCategory.Orion,
                "[Area:Transfer] BeginTransfer skipped: worker missing for {0} area={1}->{2}",
                DescribeEntity(entity),
                sourceAreaIndex,
                targetAreaIndex);
            return;
        }

        bool crossWorker = sourceWorkerId.Value != targetWorkerId.Value;

        if (server.Properties.AreaSchedulerDebug)
        {
            Log.Debug(
                LogCategory.Orion,
                "[Area:Transfer] begin {0} {1} '{2}'({3}) -> '{4}'({5}) crossWorker={6}",
                DescribeEntity(entity),
                dimension.Identifier,
                sourceShard.Name,
                sourceAreaIndex,
                targetShard.Name,
                targetAreaIndex,
                crossWorker);
        }

        AreaEntitySnapshot snapshot = AreaEntitySnapshot.Capture(
            entity,
            session,
            dimension,
            sourceAreaIndex,
            targetAreaIndex,
            crossWorker,
            position);

        if (session is null)
        {
            CrossAreaTransferHandler.InFlightMobTransfers[entity] = 1;
        }

        CrossAreaTransferHandler.MarkTransferInFlight(entity.RuntimeId);
        areaScheduler.BeginAreaTransfer(session, snapshot);
    }

    static void EnsureAreasAttached(AreaScheduler scheduler, Dimension dimension, int source, int target)
    {
        scheduler.RequestAttachArea(dimension, source);
        scheduler.RequestAttachArea(dimension, target);
    }

    static string DescribeEntity(object entity) =>
        entity is Orion.Player.Player player
            ? $"player={player.Username}"
            : entity is IAreaEntity areaEntity
                ? $"entity runtime={areaEntity.RuntimeId}"
                : $"entity={entity.GetType().Name}";
}
