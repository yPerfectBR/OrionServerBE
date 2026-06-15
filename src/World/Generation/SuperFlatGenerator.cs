using Orion.World.Block;
using Orion.Protocol.Enums;
using Orion.Protocol.Registry;
using Orion.World.Chunk;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Generation;

public sealed class SuperFlatGenerator : Generator
{
    private readonly (int Y, BlockPermutation Permutation)[] _layers;

    public override string Identifier => "superflat";

    public SuperFlatGenerator() : this(-64)
    {
    }

    public SuperFlatGenerator(int baseY)
    {
        _layers =
        [
            (baseY, BlockPermutation.Resolve(BedrockBlockStates.Bedrock)),
            (baseY + 1, BlockPermutation.Resolve(BedrockBlockStates.Dirt)),
            (baseY + 2, BlockPermutation.Resolve(BedrockBlockStates.Dirt)),
            (baseY + 3, BlockPermutation.Resolve(BedrockBlockStates.Dirt)),
            (baseY + 4, BlockPermutation.Resolve(BedrockBlockStates.GrassBlock)),
        ];
    }

    public override ChunkColumn Generate(DimensionType dimensionType, int x, int z)
    {
        ChunkColumn chunk = new(x, z, dimensionType);

        for (int i = 0; i < _layers.Length; i++)
        {
            (int y, BlockPermutation permutation) = _layers[i];
            SubChunk subChunk = chunk.GetSubChunk(y >> 4);
            BlockStorage storage = subChunk.GetLayer(0);
            int by = y & 0xF;
            int networkId = permutation.NetworkId;

            for (int lx = 0; lx < 16; lx++)
            {
                for (int lz = 0; lz < 16; lz++)
                {
                    storage.SetState(lx, by, lz, networkId);
                }
            }
        }

        chunk.Dirty = false;
        return chunk;
    }
}
