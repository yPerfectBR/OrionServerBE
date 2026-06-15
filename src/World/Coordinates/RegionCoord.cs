namespace Orion.World.Coordinates;

/// <summary>
/// Region shard coordinates within a dimension.
/// </summary>
public readonly record struct RegionCoord(int X, int Z)
{
    public static RegionCoord FromChunk(ChunkCoord chunk, int regionChunkSize) =>
        new(
            CoordMath.FloorDiv(chunk.X, regionChunkSize),
            CoordMath.FloorDiv(chunk.Z, regionChunkSize));
}
