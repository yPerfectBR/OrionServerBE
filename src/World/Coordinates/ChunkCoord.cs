namespace Orion.World.Coordinates;

/// <summary>
/// Chunk column coordinates in a dimension.
/// </summary>
public readonly record struct ChunkCoord(int X, int Z)
{
    public long Hash => CoordMath.HashChunk(X, Z);

    public static ChunkCoord FromBlock(int blockX, int blockZ) =>
        new(CoordMath.FloorDiv(blockX, 16), CoordMath.FloorDiv(blockZ, 16));

    public static ChunkCoord FromBlock(float blockX, float blockZ) =>
        FromBlock((int)MathF.Floor(blockX), (int)MathF.Floor(blockZ));

    public static ChunkCoord FromHash(long hash)
    {
        CoordMath.UnhashChunk(hash, out int x, out int z);
        return new(x, z);
    }
}
