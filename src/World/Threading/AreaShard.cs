using Orion.World.Coordinates;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Threading;

/// <summary>
/// Static spatial shard owning chunks (and later entities) for one threading area.
/// Index 0 is the default shard for coordinates outside configured areas.
/// </summary>
public sealed class AreaShard
{
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

    public int ChunkCount => _chunks.Count;
    public int EntityCount => _entities.Count;
    public IEnumerable<IAreaStoredEntity> Entities => _entities;
    public IEnumerable<ChunkColumn> Chunks => _chunks.Values;
    public IEnumerable<long> ChunkHashes => _chunks.Keys;

    public bool ContainsChunk(int chunkX, int chunkZ)
    {
        if (IsDefault)
        {
            return true;
        }

        return chunkX >= StartChunkX && chunkX <= EndChunkX
            && chunkZ >= StartChunkZ && chunkZ <= EndChunkZ;
    }

    public bool TryGetChunk(long hash, out ChunkColumn? chunk) => _chunks.TryGetValue(hash, out chunk);

    public void SetChunk(ChunkColumn chunk) =>
        _chunks[CoordMath.HashChunk(chunk.X, chunk.Z)] = chunk;

    public bool RemoveChunk(long hash, out ChunkColumn? chunk) => _chunks.Remove(hash, out chunk);

    public void AddEntity(IAreaStoredEntity entity) => _entities.Add(entity);

    public void RemoveEntity(IAreaStoredEntity entity) => _entities.Remove(entity);
}
