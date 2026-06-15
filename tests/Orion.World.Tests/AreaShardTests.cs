using Orion.Config;
using Orion.World.Threading;

namespace Orion.World.Tests;

public sealed class AreaShardTests
{
    [Fact]
    public void ResolveShard_RoutesChunksToConfiguredArea()
    {
        AreaShardManager manager = new([
            new ThreadingAreaConfig { Name = "city", Start = [0, 0], End = [10, 10] }
        ]);

        AreaShard inside = manager.ResolveShard(5, 5);
        AreaShard outside = manager.ResolveShard(20, 20);

        Assert.Equal(1, inside.AreaIndex);
        Assert.Equal("city", inside.Name);
        Assert.Equal(AreaResolver.DefaultThread, outside.AreaIndex);
    }

    [Fact]
    public void ChunksAreIsolatedPerShard()
    {
        AreaShardManager manager = new([
            new ThreadingAreaConfig { Name = "city", Start = [0, 0], End = [1, 1] }
        ]);

        AreaShard city = manager.ResolveShard(0, 0);
        AreaShard outside = manager.ResolveShard(5, 5);

        city.SetChunk(new Orion.World.Chunk.Chunk(0, 0, Orion.Protocol.Enums.DimensionType.Overworld));

        Assert.Equal(1, city.ChunkCount);
        Assert.Equal(0, outside.ChunkCount);
    }
}
