using Orion.Player;
namespace Orion.Network;

using Orion.Api;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.Player;
using Orion.World;


/// <summary>
/// Spatial broadcast fan-out for dimension packets (Phase 6c).
/// </summary>
public static class BroadcastService
{
    public static void Broadcast(Dimension dimension, DataPacket packet, BroadcastOptions? options = null)
    {
        if (dimension.World?.Server is not global::Orion.Server server)
        {
            return;
        }

        BroadcastOptions resolved = options ?? new BroadcastOptions();
        resolved.Center ??= DimensionGameplayExtensions.GetPacketPosition(packet);
        float radiusSquared = resolved.Radius * resolved.Radius;

        if (packet is RemoveActorPacket removeActor)
        {
            InvalidateVisibleEntityForRemove(dimension, server, removeActor.EntityUniqueId, resolved.Except);
        }
        else if (packet is TakeItemActorPacket takeItem)
        {
            // TakeItemActor removes the item client-side; drop visibility tracking so we
            // do not leave a stale entry that skips a later RemoveActor.
            InvalidateVisibleEntity(dimension, server, takeItem.ItemEntityRuntimeId, resolved.Except);
        }

        // Always enumerate live sessions. The spatial index is an optimization that can miss
        // players not yet indexed (or dropped from the chunk map), which breaks multiplayer FX
        // like block-crack LevelEvents while the breaker still sees their own copy.
        List<PlayerSession> candidates = GetSessionsInRadius(dimension, server, resolved.Center, resolved.Radius);

        foreach (PlayerSession session in candidates)
        {
            if (session.ActiveEntity is not Player player || player.Dimension != dimension)
            {
                continue;
            }

            if (resolved.Except is not null && resolved.Except.Contains(player))
            {
                continue;
            }

            if (resolved.Center.HasValue)
            {
                Vec3f playerPosition = player.Position;
                Vec3f centerPosition = resolved.Center.Value;
                float dx = playerPosition.X - centerPosition.X;
                float dy = playerPosition.Y - centerPosition.Y;
                float dz = playerPosition.Z - centerPosition.Z;
                float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }
            }

            // Clone LevelEvent per recipient so concurrent session workers never share one instance.
            DataPacket outbound = packet is LevelEventPacket levelEvent
                ? levelEvent with { }
                : packet;

            if (server.ConnectionCoordinator is Scheduling.ConnectionCoordinator coordinator && coordinator.IsActive)
            {
                coordinator.EnqueueViewDelta(session, outbound);
            }
            else
            {
                SessionSendCoordinator.Send(session, outbound);
            }
        }
    }

    static List<PlayerSession> GetSessionsInRadius(
        Dimension dimension,
        global::Orion.Server server,
        Vec3f? center,
        float radius)
    {
        if (!center.HasValue)
        {
            return [.. server.Sessions.Values];
        }

        float radiusSquared = radius * radius;
        List<PlayerSession> results = [];
        foreach (PlayerSession session in server.Sessions.Values)
        {
            if (session.ActiveEntity is not Player player || player.Dimension != dimension)
            {
                continue;
            }

            Vec3f position = player.Position;
            float dx = position.X - center.Value.X;
            float dy = position.Y - center.Value.Y;
            float dz = position.Z - center.Value.Z;
            float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
            if (distanceSquared <= radiusSquared)
            {
                results.Add(session);
            }
        }

        return results;
    }

    static void InvalidateVisibleEntityForRemove(
        Dimension dimension,
        global::Orion.Server server,
        long entityUniqueId,
        global::Orion.Entity.Entity[]? except)
    {
        foreach (PlayerSession session in server.Sessions.Values)
        {
            if (session.ActiveEntity is not Player observer || observer.Dimension != dimension)
            {
                continue;
            }

            if (except is not null && Array.IndexOf(except, observer) >= 0)
            {
                continue;
            }

            observer.GetTrait<IPlayerChunkView>()?.InvalidateVisibleEntityByUniqueId(entityUniqueId);
        }
    }

    static void InvalidateVisibleEntity(
        Dimension dimension,
        global::Orion.Server server,
        ulong runtimeId,
        global::Orion.Entity.Entity[]? except)
    {
        foreach (PlayerSession session in server.Sessions.Values)
        {
            if (session.ActiveEntity is not Player observer || observer.Dimension != dimension)
            {
                continue;
            }

            if (except is not null && Array.IndexOf(except, observer) >= 0)
            {
                continue;
            }

            observer.GetTrait<IPlayerChunkView>()?.InvalidateVisibleEntity(runtimeId);
        }
    }
}
