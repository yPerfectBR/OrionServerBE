using Orion.Protocol.Enums;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Generation;

public abstract class Generator
{
    public abstract string Identifier { get; }

    public abstract ChunkColumn Generate(DimensionType dimensionType, int x, int z);

    public virtual void Populate(ChunkColumn chunk)
    {
    }
}
