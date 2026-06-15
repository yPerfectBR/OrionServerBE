using Orion.Config;
using Orion.Protocol.Enums;
using Orion.World.Generation;
using Orion.World.Pregeneration;
using Orion.World.Provider;

namespace Orion.World.Tests;

public sealed class ChunkPregeneratorTests
{
    [Fact]
    public void Pregenerate_MemoryLock_KeepsChunksInCache()
    {
        using InMemoryProvider provider = new();
        Dimension dimension = new("overworld", DimensionType.Overworld, provider, new SuperFlatGenerator());
        ChunkPregenerator pregenerator = new();

        pregenerator.Pregenerate(dimension, new ChunkPregenerationConfig
        {
            Start = [0, 0],
            End = [1, 1],
            MemoryLock = true
        });

        Assert.Equal(4, dimension.ChunkCount);
        Assert.True(dimension.HasChunk(0, 0));
        Assert.True(dimension.HasChunk(1, 1));
    }

    [Fact]
    public void Pregenerate_WithoutMemoryLock_UnloadsAfterSave()
    {
        using InMemoryProvider provider = new();
        Dimension dimension = new("overworld", DimensionType.Overworld, provider, new SuperFlatGenerator());
        ChunkPregenerator pregenerator = new();

        pregenerator.Pregenerate(dimension, new ChunkPregenerationConfig
        {
            Start = [0, 0],
            End = [0, 0],
            MemoryLock = false
        });

        Assert.Equal(0, dimension.ChunkCount);
        Assert.True(provider.HasChunk(DimensionType.Overworld, 0, 0));
    }
}
