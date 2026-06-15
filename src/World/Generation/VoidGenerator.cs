using Orion.Protocol.Enums;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Generation;

public sealed class VoidGenerator : Generator
{
    public override string Identifier => "void";

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z) =>
        new(x, z, dimensionType);
}
