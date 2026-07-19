using Orion.World.Coordinates;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Threading;

/// <summary>
/// Static spatial shard owning chunks and entities for one threading area.
/// Index 0 is the default shard for coordinates outside configured areas.
/// Mutated from area workers and session/network paths — all access is synchronized.
/// </summary>
public sealed class AreaShard
{
    private readonly object _sync = new();
    private readonly Dictionary<long, ChunkColumn> _chunks = [];
    private readonly HashSet<IAreaStoredEntity> _entities = [];

    public int AreaIndex { get; }
    public string Name { get; }
    public int StartChunkX { get; }
    public int StartChunkZ { get; }
    public int EndChunkX { get; }
    public int EndChunkZ { get; }
    public bool IsDefault => AreaIndex == AreaResolver.DefaultThread;

    public int? AttachedWorkerId { get; set; }
    public int PresenceCount { get; set; }
    public bool IsAttached => AttachedWorkerId.HasValue;

    public AreaShard(int areaIndex, string name, int startChunkX, int startChunkZ, int endChunkX, int endChunkZ)
    {
        AreaIndex = areaIndex;
        Name = name;
        StartChunkX = startChunkX;
        StartChunkZ = startChunkZ;
        EndChunkX = endChunkX;
        EndChunkZ = endChunkZ;
    }

    public static AreaShard CreateDefault() =>
        new(AreaResolver.DefaultThread, "outside", int.MinValue, int.MinValue, int.MaxValue, int.MaxValue);

    public int ChunkCount
    {
        get
        {
            lock (_sync)
            {
                return _chunks.Count;
            }
        }
    }

    public int EntityCount
    {
        get
        {
            lock (_sync)
            {
                return _entities.Count;
            }
        }
    }

    /// <summary>Thread-safe snapshot of chunk columns (preferred over enumerating <see cref="Chunks"/>).</summary>
    public ChunkColumn[] SnapshotChunks()
    {
        lock (_sync)
        {
            if (_chunks.Count == 0)
            {
                return [];
            }

            ChunkColumn[] snapshot = new ChunkColumn[_chunks.Count];
            _chunks.Values.CopyTo(snapshot, 0);
            return snapshot;
        }
    }

    /// <summary>Thread-safe snapshot of stored entities.</summary>
    public IAreaStoredEntity[] SnapshotEntities()
    {
        lock (_sync)
        {
            if (_entities.Count == 0)
            {
                return [];
            }

            IAreaStoredEntity[] snapshot = new IAreaStoredEntity[_entities.Count];
            _entities.CopyTo(snapshot);
            return snapshot;
        }
    }

    public IEnumerable<IAreaStoredEntity> Entities => SnapshotEntities();
    public IEnumerable<ChunkColumn> Chunks => SnapshotChunks();
    public IEnumerable<long> ChunkHashes
    {
        get
        {
            lock (_sync)
            {
                return _chunks.Keys.ToArray();
            }
        }
    }

    public bool ContainsChunk(int chunkX, int chunkZ)
    {
        if (IsDefault)
        {
            return true;
        }

        return chunkX >= StartChunkX && chunkX <= EndChunkX
            && chunkZ >= StartChunkZ && chunkZ <= EndChunkZ;
    }

    public bool TryGetChunk(long hash, out ChunkColumn? chunk)
    {
        lock (_sync)
        {
            return _chunks.TryGetValue(hash, out chunk);
        }
    }

    public void SetChunk(ChunkColumn chunk)
    {
        lock (_sync)
        {
            _chunks[CoordMath.HashChunk(chunk.X, chunk.Z)] = chunk;
        }
    }

    public bool RemoveChunk(long hash, out ChunkColumn? chunk)
    {
        lock (_sync)
        {
            return _chunks.Remove(hash, out chunk);
        }
    }

    public void AddEntity(IAreaStoredEntity entity)
    {
        lock (_sync)
        {
            _entities.Add(entity);
        }
    }

    public void RemoveEntity(IAreaStoredEntity entity)
    {
        lock (_sync)
        {
            _entities.Remove(entity);
        }
    }
}
