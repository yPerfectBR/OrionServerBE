using Orion.Protocol.Enums;
using Orion.Protocol.Registry;
using Orion.World.Generation;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Tests;

public sealed class SuperFlatGeneratorTests
{
    [Fact]
    public void Generate_ProducesExpectedSuperFlatLayers()
    {
        SuperFlatGenerator generator = new();
        ChunkColumn chunk = generator.Generate(DimensionType.Overworld, 0, 0);

        Assert.Equal(BedrockBlockStates.Bedrock, chunk.GetPermutation(0, -64, 0).NetworkId);
        Assert.Equal(BedrockBlockStates.Dirt, chunk.GetPermutation(0, -63, 0).NetworkId);
        Assert.Equal(BedrockBlockStates.Dirt, chunk.GetPermutation(0, -62, 0).NetworkId);
        Assert.Equal(BedrockBlockStates.Dirt, chunk.GetPermutation(0, -61, 0).NetworkId);
        Assert.Equal(BedrockBlockStates.GrassBlock, chunk.GetPermutation(0, -60, 0).NetworkId);
        Assert.False(chunk.Dirty);
    }
}
