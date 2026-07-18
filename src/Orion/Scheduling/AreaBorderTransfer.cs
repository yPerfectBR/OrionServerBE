using System.Collections.Concurrent;
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

    public static bool TryAfterTeleport(Server server, IAreaEntity player, Vec3f targetPosition)
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
            ?? dimension.ResolveAreaIndex(player.Position.X, player.Position.Z);
        int targetAreaIndex = dimension.ResolveAreaIndex(targetPosition.X, targetPosition.Z);
        if (sourceAreaIndex == targetAreaIndex)
        {
            return false;
        }

        LastTransferTickByRuntimeId.TryRemove(player.RuntimeId, out _);
        BeginTransfer(server, dimension, player, session, sourceAreaIndex, targetAreaIndex, targetPosition);
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
            AreaTransferLog.Warn(
                session,
                $"skipped {AreaTransferLog.DescribeEntity(entity)} {dimension.Identifier} " +
                $"'{sourceShard.Name}'({sourceAreaIndex}) -> '{targetShard.Name}'({targetAreaIndex}): " +
                $"worker missing (source={sourceWorkerId?.ToString() ?? "none"}, target={targetWorkerId?.ToString() ?? "none"}) " +
                $"tick={dimension.World?.TickValue ?? 0}");
            return;
        }

        bool crossWorker = sourceWorkerId.Value != targetWorkerId.Value;
        AreaTransferLog.Info(
            session,
            $"begin {AreaTransferLog.DescribeEntity(entity)} {dimension.Identifier} " +
            $"'{sourceShard.Name}'({sourceAreaIndex}) -> '{targetShard.Name}'({targetAreaIndex}) " +
            $"aw{sourceWorkerId.Value}->aw{targetWorkerId.Value} crossWorker={crossWorker} " +
            $"pos=({position.X:F1},{position.Y:F1},{position.Z:F1}) tick={dimension.World?.TickValue ?? 0}");

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

        areaScheduler.BeginAreaTransfer(session, snapshot);
    }

    static void EnsureAreasAttached(AreaScheduler scheduler, Dimension dimension, int source, int target)
    {
        scheduler.RequestAttachArea(dimension, source);
        scheduler.RequestAttachArea(dimension, target);
    }
}
