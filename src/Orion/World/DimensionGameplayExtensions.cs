using System.Runtime.CompilerServices;
using System.Text;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Block;
using Orion.Entity;
using Orion.Player;
using Orion.Scheduling;
using Orion.Protocol.Types;
using Orion.World.Block;
using Orion.World.Chunk;
using Orion.World.Coordinates;
using Orion.World.Threading;
using GameplayBlock = Orion.Block.Block;
using GameplayPermutation = Orion.Block.BlockPermutation;
using WorldPermutation = Orion.World.Block.BlockPermutation;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World;

public static class DimensionGameplayExtensions
{
    private static readonly ConditionalWeakTable<Dimension, DimensionGameplayState> States = new();

    public static SpatialPlayerIndex GetSpatialIndex(this Dimension dimension) =>
        GetState(dimension).SpatialIndex;


    public static IEnumerable<global::Orion.Entity.Entity> GetEntities(this Dimension dimension)
    {
        foreach (AreaShard shard in dimension.ShardManager.Shards)
        {
            foreach (IAreaStoredEntity stored in shard.Entities)
            {
                if (stored is global::Orion.Entity.Entity entity)
                {
                    yield return entity;
                }
            }
        }
    }

    public static Difficulty GetDifficulty(this Dimension dimension)
    {
        _ = dimension;
        return Difficulty.Normal;
    }

    public static GameplayBlock? GetBlock(this Dimension dimension, int x, int y, int z, int layer = 0)
    {
        WorldPermutation worldPerm = dimension.GetPermutation(x, y, z, layer);
        GameplayPermutation perm = GameplayPermutation.Permutations.TryGetValue(worldPerm.NetworkId, out GameplayPermutation known)
            ? known
            : GameplayPermutation.Resolve(worldPerm.NetworkId, []);
        return new GameplayBlock(perm.Type, perm);
    }

    public static void SetBlock(this Dimension dimension, int x, int y, int z, GameplayBlock block, int layer = 0, bool dirty = true)
    {
        WorldPermutation worldPerm = WorldPermutation.Resolve(block.Permutation.NetworkId);
        dimension.SetPermutation(x, y, z, worldPerm, layer, dirty);
    }

    public static void SetGameplayPermutation(
        this Dimension dimension,
        int x,
        int y,
        int z,
        GameplayPermutation permutation,
        int layer = 0,
        bool dirty = true)
    {
        dimension.SetPermutation(x, y, z, permutation.ToWorld(), layer, dirty);
    }

    public static GameplayPermutation GetGameplayPermutation(this Dimension dimension, int x, int y, int z, int layer = 0) =>
        dimension.GetPermutation(x, y, z, layer).ToGameplay();

    public static void RequestChunks(this Dimension dimension, ReadOnlySpan<(int X, int Z)> chunks, Action<ChunkColumn> ready)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            (int x, int z) = chunks[i];
            ChunkColumn chunk = dimension.GetOrCreateChunk(x, z);
            ready(chunk);
        }
    }

    public static void AddChunkViewer(this Dimension dimension, int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        DimensionGameplayState state = GetState(dimension);
        state.ChunkViewers[hash] = state.ChunkViewers.TryGetValue(hash, out int count) ? count + 1 : 1;
    }

    public static bool RemoveChunkViewer(this Dimension dimension, int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        DimensionGameplayState state = GetState(dimension);
        if (!state.ChunkViewers.TryGetValue(hash, out int count))
        {
            return false;
        }

        if (count <= 1)
        {
            state.ChunkViewers.Remove(hash);
        }
        else
        {
            state.ChunkViewers[hash] = count - 1;
        }

        return true;
    }

    public static bool HasChunkViewers(this Dimension dimension, int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        return GetState(dimension).ChunkViewers.ContainsKey(hash);
    }

    public static bool TryGetPlayerAreaStats(
        this Dimension dimension,
        float blockX,
        float blockZ,
        out int areaIndex,
        out int areaChunks,
        out int areaEntities)
    {
        areaIndex = dimension.ResolveAreaIndex(blockX, blockZ);
        AreaShard shard = dimension.GetAreaShard(areaIndex);
        areaChunks = shard.ChunkCount;
        areaEntities = shard.EntityCount;
        return true;
    }

    /// <summary>
    /// Position used for spatial broadcast filtering. Returns null for packets without a
    /// meaningful world position so they are delivered to all players in the dimension
    /// (Basalt behaviour) — critical for RemoveActor / TakeItemActor / AddItemActor.
    /// </summary>
    public static Vec3f? GetPacketPosition(DataPacket packet) => packet switch
    {
        MovePlayerPacket move => move.Position,
        MoveActorAbsolutePacket absolute => absolute.Position,
        MoveActorDeltaPacket delta => delta.Position,
        AddItemActorPacket addItem => addItem.Position,
        AddActorPacket addActor => addActor.Position,
        AddPlayerPacket addPlayer => addPlayer.Position,
        LevelEventPacket levelEvent => levelEvent.Position,
        UpdateBlockPacket update => new Vec3f { X = update.Position.X, Y = update.Position.Y, Z = update.Position.Z },
        _ => null
    };

    static DimensionGameplayState GetState(Dimension dimension) =>
        States.GetValue(dimension, static _ => new DimensionGameplayState());
}

public sealed class SpatialPlayerIndex
{
    private readonly Dictionary<long, HashSet<PlayerSession>> _chunkSessions = [];
    private readonly Dictionary<PlayerSession, long> _sessionChunks = [];

    public int SessionCount => _sessionChunks.Count;

    public void SetPlayerChunk(PlayerSession session, ChunkCoord chunk)
    {
        long hash = chunk.Hash;

        if (_sessionChunks.TryGetValue(session, out long previousHash))
        {
            if (previousHash == hash)
            {
                return;
            }

            RemoveSessionFromChunk(session, previousHash);
        }

        _sessionChunks[session] = hash;

        if (!_chunkSessions.TryGetValue(hash, out HashSet<PlayerSession>? sessions))
        {
            sessions = [];
            _chunkSessions[hash] = sessions;
        }

        sessions.Add(session);
    }

    public void RemovePlayer(PlayerSession session)
    {
        if (!_sessionChunks.Remove(session, out long hash))
        {
            return;
        }

        RemoveSessionFromChunk(session, hash);
    }

    public List<PlayerSession> GetSessionsInRadius(Vec3f center, float radius)
    {
        if (_sessionChunks.Count == 0)
        {
            return [];
        }

        float radiusSquared = radius * radius;
        int chunkRadius = (int)MathF.Ceiling(radius / 16f);
        ChunkCoord centerChunk = ChunkCoord.FromBlock(center.X, center.Z);
        List<PlayerSession> results = [];

        for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
        {
            for (int dz = -chunkRadius; dz <= chunkRadius; dz++)
            {
                ChunkCoord chunk = new(centerChunk.X + dx, centerChunk.Z + dz);
                if (!_chunkSessions.TryGetValue(chunk.Hash, out HashSet<PlayerSession>? sessions))
                {
                    continue;
                }

                foreach (PlayerSession session in sessions)
                {
                    if (session.ActiveEntity is not global::Orion.Player.Player player)
                    {
                        continue;
                    }

                    Vec3f position = player.Position;
                    float dxPos = position.X - center.X;
                    float dyPos = position.Y - center.Y;
                    float dzPos = position.Z - center.Z;
                    float distanceSquared = (dxPos * dxPos) + (dyPos * dyPos) + (dzPos * dzPos);
                    if (distanceSquared > radiusSquared || results.Contains(session))
                    {
                        continue;
                    }

                    results.Add(session);
                }
            }
        }

        return results;
    }

    void RemoveSessionFromChunk(PlayerSession session, long hash)
    {
        if (!_chunkSessions.TryGetValue(hash, out HashSet<PlayerSession>? sessions))
        {
            return;
        }

        sessions.Remove(session);
        if (sessions.Count == 0)
        {
            _chunkSessions.Remove(hash);
        }
    }
}


sealed class DimensionGameplayState
{
    public SpatialPlayerIndex SpatialIndex { get; } = new();
    public Dictionary<long, int> ChunkViewers { get; } = [];
}

public static class BlockPermutationBridge
{
    public static GameplayPermutation ToGameplay(this WorldPermutation world) =>
        GameplayPermutation.Permutations.TryGetValue(world.NetworkId, out GameplayPermutation perm)
            ? perm
            : GameplayPermutation.Resolve(world.NetworkId, []);

    public static WorldPermutation ToWorld(this GameplayPermutation gameplay) =>
        WorldPermutation.Resolve(gameplay.NetworkId);
}
