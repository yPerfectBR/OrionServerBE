using System.Collections.Concurrent;
using Orion.Player;
using Orion.World;
using Orion.World.Coordinates;
using Orion.World.Threading;

namespace Orion.Scheduling;

public static class AreaPlayerPresence
{
    private static readonly ConcurrentDictionary<PlayerSession, HashSet<int>> HaloBySession = new();

    public static void SyncViewHalo(
        Server server,
        Dimension dimension,
        IAreaEntity player,
        int viewDistanceChunks,
        int simulationDistanceChunks)
    {
        if (!server.Properties.AreaThreadingEnabled || !server.AreaScheduler.IsActive)
        {
            return;
        }

        PlayerSession? session = player is IPlayerWithSession playerWithSession ? playerWithSession.Session : null;
        if (session is null)
        {
            return;
        }

        int haloRadius = Math.Max(viewDistanceChunks, simulationDistanceChunks);
        HashSet<int> desired = ComputeHaloAreas(
            player.Position.X,
            player.Position.Z,
            haloRadius,
            dimension.ShardManager);

        HashSet<int> previous = HaloBySession.GetOrAdd(session, static _ => []);
        ApplyHaloDiff(server, dimension, previous, desired);
        HaloBySession[session] = desired;
    }

    public static void OnPlayerEnteredArea(Server server, Dimension dimension, int areaIndex)
    {
        if (!server.Properties.AreaThreadingEnabled)
        {
            return;
        }

        AreaShard area = dimension.GetAreaShard(areaIndex);
        if (area.PresenceCount == 0)
        {
            server.AreaScheduler.RequestAttachArea(dimension, areaIndex);
        }

        area.PresenceCount++;
    }

    public static void OnPlayerLeftArea(Server server, Dimension dimension, int areaIndex)
    {
        if (!server.Properties.AreaThreadingEnabled)
        {
            return;
        }

        AreaShard area = dimension.GetAreaShard(areaIndex);
        if (area.PresenceCount <= 0)
        {
            return;
        }

        area.PresenceCount--;

        if (area.PresenceCount == 0)
        {
            server.AreaScheduler.RequestDetachArea(dimension, areaIndex);
        }
    }

    public static bool TryGetHaloAreas(PlayerSession session, out IReadOnlyCollection<int> areas)
    {
        if (HaloBySession.TryGetValue(session, out HashSet<int>? halo))
        {
            areas = halo;
            return true;
        }

        areas = [];
        return false;
    }

    public static void ClearSession(Server server, Dimension? dimension, PlayerSession session)
    {
        if (!HaloBySession.TryRemove(session, out HashSet<int>? previous)
            || dimension is null
            || !server.Properties.AreaThreadingEnabled)
        {
            return;
        }

        foreach (int areaIndex in previous)
        {
            OnPlayerLeftArea(server, dimension, areaIndex);
        }
    }

    internal static HashSet<int> ComputeHaloAreas(
        float blockX,
        float blockZ,
        int chunkRadius,
        AreaShardManager manager)
    {
        ChunkCoord centerChunk = ChunkCoord.FromBlock(blockX, blockZ);
        int minChunkX = centerChunk.X - chunkRadius;
        int maxChunkX = centerChunk.X + chunkRadius;
        int minChunkZ = centerChunk.Z - chunkRadius;
        int maxChunkZ = centerChunk.Z + chunkRadius;

        HashSet<int> areas = [];
        foreach (AreaShard shard in manager.Shards)
        {
            if (ShardIntersectsHalo(shard, minChunkX, maxChunkX, minChunkZ, maxChunkZ))
            {
                areas.Add(shard.AreaIndex);
            }
        }

        return areas;
    }

    static bool ShardIntersectsHalo(AreaShard shard, int minChunkX, int maxChunkX, int minChunkZ, int maxChunkZ)
    {
        if (shard.IsDefault)
        {
            return true;
        }

        return shard.StartChunkX <= maxChunkX
            && shard.EndChunkX >= minChunkX
            && shard.StartChunkZ <= maxChunkZ
            && shard.EndChunkZ >= minChunkZ;
    }

    static void ApplyHaloDiff(
        Server server,
        Dimension dimension,
        HashSet<int> previous,
        HashSet<int> desired)
    {
        foreach (int areaIndex in previous)
        {
            if (!desired.Contains(areaIndex))
            {
                OnPlayerLeftArea(server, dimension, areaIndex);
            }
        }

        foreach (int areaIndex in desired)
        {
            if (!previous.Contains(areaIndex))
            {
                OnPlayerEnteredArea(server, dimension, areaIndex);
            }
        }
    }
}

public interface IPlayerWithSession
{
    PlayerSession? Session { get; }
}
