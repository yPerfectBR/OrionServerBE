using Orion.World.Block;
using Orion.Protocol.Enums;
using Orion.World.Coordinates;
using Orion.World.Generation;
using Orion.World.Provider;
using Orion.World.Threading;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World;

/// <summary>
/// Slim dimension with area-sharded chunk cache, provider I/O, and world generator.
/// </summary>
public sealed class Dimension : IDisposable
{
    private readonly AreaShardManager _shardManager;
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private bool _disposed;

    public string Identifier { get; }
    public DimensionType Type { get; }
    public World? World { get; internal set; }
    public DimensionGameRules Gamerules { get; } = new();
    public AreaShardManager ShardManager => _shardManager;

    public int ChunkCount => _shardManager.TotalChunkCount;

    public Dimension(
        string identifier,
        DimensionType type,
        WorldProvider provider,
        Generator? generator = null,
        IReadOnlyList<Orion.Config.ThreadingAreaConfig>? threadingAreas = null)
    {
        Identifier = identifier;
        Type = type;
        _provider = provider;
        _generator = generator ?? new VoidGenerator();
        _shardManager = new AreaShardManager(threadingAreas ?? []);
    }

    public bool HasChunk(int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        AreaShard shard = _shardManager.ResolveShard(x, z);
        return shard.TryGetChunk(hash, out _) || _provider.HasChunk(Type, x, z);
    }

    public ChunkColumn? GetChunk(int x, int z) => GetOrLoadChunk(x, z);

    public ChunkColumn GetOrCreateChunk(int x, int z)
    {
        ChunkColumn? chunk = GetOrLoadChunk(x, z);
        if (chunk is not null)
        {
            return chunk;
        }

        AreaShard shard = _shardManager.ResolveShard(x, z);
        chunk = _generator.Generate(Type, x, z);
        _generator.Populate(chunk);
        chunk.Dirty = true;
        shard.SetChunk(chunk);
        return chunk;
    }

    public ChunkColumn? GetOrLoadChunk(int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        AreaShard shard = _shardManager.ResolveShard(x, z);
        if (shard.TryGetChunk(hash, out ChunkColumn? cached))
        {
            return cached;
        }

        ChunkColumn? chunk = _provider.LoadChunk(Type, x, z);
        if (chunk is not null)
        {
            shard.SetChunk(chunk);
            return chunk;
        }

        return null;
    }

    public void SetChunk(ChunkColumn chunk)
    {
        AreaShard shard = _shardManager.ResolveShard(chunk.X, chunk.Z);
        shard.SetChunk(chunk);
        _provider.SaveChunk(chunk);
    }

    public bool SaveChunk(int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        AreaShard shard = _shardManager.ResolveShard(x, z);
        if (!shard.TryGetChunk(hash, out ChunkColumn? chunk) || chunk is null)
        {
            return false;
        }

        _provider.SaveChunk(chunk);
        chunk.Dirty = false;
        return true;
    }

    public bool UnloadChunk(int x, int z, bool save = true)
    {
        long hash = CoordMath.HashChunk(x, z);
        AreaShard shard = _shardManager.ResolveShard(x, z);
        if (!shard.TryGetChunk(hash, out ChunkColumn? chunk) || chunk is null)
        {
            return false;
        }

        if (save && chunk.Dirty)
        {
            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }

        chunk.ReleaseMemory();
        return shard.RemoveChunk(hash, out _);
    }

    public void SaveDirtyChunks()
    {
        for (int i = 0; i < _shardManager.ShardCount; i++)
        {
            SaveDirtyChunks(_shardManager.GetShard(i));
        }
    }

    /// <summary>Persists dirty columns owned by a single shard (call from the owning area worker).</summary>
    public void SaveDirtyChunks(AreaShard shard)
    {
        ArgumentNullException.ThrowIfNull(shard);
        ChunkColumn[] chunks = shard.SnapshotChunks();
        for (int i = 0; i < chunks.Length; i++)
        {
            ChunkColumn chunk = chunks[i];
            if (!chunk.Dirty)
            {
                continue;
            }

            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }
    }

    public IEnumerable<ChunkColumn> GetLoadedChunks() => _shardManager.AllChunks;

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), layer);
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetPermutation(GetChunkLocal(x), y, GetChunkLocal(z), permutation, layer, dirty);
    }

    public int GetBiome(int x, int y, int z)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        return chunk.GetBiome(GetChunkLocal(x), y, GetChunkLocal(z));
    }

    public void SetBiome(int x, int y, int z, int biomeId, bool dirty = true)
    {
        ChunkColumn chunk = GetOrCreateChunk(x >> 4, z >> 4);
        chunk.SetBiome(GetChunkLocal(x), y, GetChunkLocal(z), biomeId, dirty);
    }

    public void Tick(ulong currentTick, uint deltaTick)
    {
        // Dirty chunk persistence is owned by each AreaWorker for its attached shards
        // (see AreaWorker.SaveAttachedDirtyChunks) to avoid cross-thread Dictionary races.
        _ = currentTick;
        _ = deltaTick;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SaveDirtyChunks();
    }

    private static int GetChunkLocal(int value) => value & 0xF;
}
