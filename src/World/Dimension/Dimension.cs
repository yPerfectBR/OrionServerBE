using Orion.World.Block;
using Orion.Protocol.Enums;
using Orion.World.Coordinates;
using Orion.World.Generation;
using Orion.World.Provider;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World;

/// <summary>
/// Slim dimension: chunk cache, provider I/O, and generator fallback only.
/// </summary>
public sealed class Dimension : IDisposable
{
    private readonly Dictionary<long, ChunkColumn> _chunks = [];
    private readonly WorldProvider _provider;
    private readonly Generator _generator;
    private bool _disposed;

    public string Identifier { get; }
    public DimensionType Type { get; }
    public World? World { get; internal set; }
    public DimensionGameRules Gamerules { get; } = new();

    public int ChunkCount => _chunks.Count;

    public Dimension(string identifier, DimensionType type, WorldProvider provider, Generator? generator = null)
    {
        Identifier = identifier;
        Type = type;
        _provider = provider;
        _generator = generator ?? new VoidGenerator();
    }

    public bool HasChunk(int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        return _chunks.ContainsKey(hash) || _provider.HasChunk(Type, x, z);
    }

    public ChunkColumn? GetChunk(int x, int z) => GetOrLoadChunk(x, z);

    public ChunkColumn GetOrCreateChunk(int x, int z)
    {
        ChunkColumn? chunk = GetOrLoadChunk(x, z);
        if (chunk is not null)
        {
            return chunk;
        }

        long hash = CoordMath.HashChunk(x, z);
        chunk = _generator.Generate(Type, x, z);
        _generator.Populate(chunk);
        chunk.Dirty = true;
        _chunks[hash] = chunk;
        return chunk;
    }

    public ChunkColumn? GetOrLoadChunk(int x, int z)
    {
        long hash = CoordMath.HashChunk(x, z);
        if (_chunks.TryGetValue(hash, out ChunkColumn? cached))
        {
            return cached;
        }

        ChunkColumn? chunk = _provider.LoadChunk(Type, x, z);
        if (chunk is not null)
        {
            _chunks[hash] = chunk;
            return chunk;
        }

        return null;
    }

    public void SetChunk(ChunkColumn chunk)
    {
        _chunks[CoordMath.HashChunk(chunk.X, chunk.Z)] = chunk;
        _provider.SaveChunk(chunk);
    }

    public bool SaveChunk(int x, int z)
    {
        if (!_chunks.TryGetValue(CoordMath.HashChunk(x, z), out ChunkColumn? chunk))
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
        if (!_chunks.TryGetValue(hash, out ChunkColumn? chunk))
        {
            return false;
        }

        if (save && chunk.Dirty)
        {
            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }

        chunk.ReleaseMemory();
        return _chunks.Remove(hash);
    }

    public void SaveDirtyChunks()
    {
        foreach (ChunkColumn chunk in _chunks.Values)
        {
            if (!chunk.Dirty)
            {
                continue;
            }

            _provider.SaveChunk(chunk);
            chunk.Dirty = false;
        }
    }

    public IEnumerable<ChunkColumn> GetLoadedChunks() => _chunks.Values;

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
        if (currentTick % 20 == 0 && _chunks.Count > 0)
        {
            SaveDirtyChunks();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SaveDirtyChunks();
        _chunks.Clear();
    }

    private static int GetChunkLocal(int value) => value & 0xF;
}
