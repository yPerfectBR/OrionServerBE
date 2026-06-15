using System.Runtime.InteropServices;
using Orion.Protocol.Enums;
using Orion.World.Coordinates;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Provider;

public abstract class WorldProvider : IDisposable
{
    public abstract string Identifier { get; }

    public abstract bool HasChunk(DimensionType dimensionType, int x, int z);

    public abstract ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z);

    public abstract void SaveChunk(ChunkColumn chunk);

    public abstract void DeleteChunk(DimensionType dimensionType, int x, int z);

    public abstract void Dispose();

    protected static long HashChunk(int x, int z) => CoordMath.HashChunk(x, z);
}
