using Orion.Player;
namespace Orion.Network;

using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.Player;
using Orion.Player.Traits;
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
            PlayerChunkRenderingTrait.InvalidateVisibleEntityForRemove(
                dimension,
                removeActor.EntityUniqueId,
                resolved.Except);
        }

        List<PlayerSession> candidates = resolved.Center.HasValue
            ? GetSessionsInRadius(dimension, server, resolved.Center.Value, resolved.Radius)
            : [.. server.Sessions.Values];

        int sentCount = 0;

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

            if (server.ConnectionCoordinator is Scheduling.ConnectionCoordinator coordinator && coordinator.IsActive)
            {
                coordinator.EnqueueViewDelta(session, packet);
            }
            else
            {
                SessionSendCoordinator.Send(session, packet);
            }

            sentCount++;
        }

    }

    static List<PlayerSession> GetSessionsInRadius(
        Dimension dimension,
        global::Orion.Server server,
        Vec3f center,
        float radius)
    {
        if (dimension.GetSpatialIndex().SessionCount > 0)
        {
            return dimension.GetSpatialIndex().GetSessionsInRadius(center, radius);
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
            float dx = position.X - center.X;
            float dy = position.Y - center.Y;
            float dz = position.Z - center.Z;
            float distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
            if (distanceSquared <= radiusSquared)
            {
                results.Add(session);
            }
        }

        return results;
    }
}
