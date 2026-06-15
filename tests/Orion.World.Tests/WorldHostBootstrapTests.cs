using Orion.Config;
using Orion.World;

namespace Orion.World.Tests;

public sealed class WorldHostBootstrapTests
{
    [Fact]
    public void Bootstrap_SetsCanAcceptPlayersAfterPregeneration()
    {
        string worldsRoot = Path.Combine(Path.GetTempPath(), "orion-world-tests", Guid.NewGuid().ToString("N"));
        string worldDir = Path.Combine(worldsRoot, "default");
        Directory.CreateDirectory(worldDir);

        var config = new OrionConfig
        {
            Server = new ServerSection
            {
                Orion = new OrionSection { SpawnWorldIdentifier = "default", TicksPerSecond = 20 },
                WorldDefaultSettings = new WorldProperties
                {
                    Identifier = "default",
                    Dimensions =
                    [
                        new DimensionConfig
                        {
                            Identifier = "overworld",
                            ChunkPregeneration =
                            [
                                new ChunkPregenerationConfig
                                {
                                    Start = [0, 0],
                                    End = [1, 1],
                                    MemoryLock = false
                                }
                            ]
                        }
                    ]
                }
            }
        };

        try
        {
            OrionInfo.SetCanAcceptPlayers(false);
            using WorldHost host = WorldHost.Bootstrap(config, worldsRoot, startScheduler: false);

            Assert.True(host.IsPregenerationComplete);
            Assert.True(host.CanAcceptPlayers);
            Assert.True(OrionInfo.CanAcceptPlayers);
        }
        finally
        {
            OrionInfo.SetCanAcceptPlayers(true);
        }
    }

    [Fact]
    public void Dispose_BlocksFurtherPlayerConnections()
    {
        string worldsRoot = Path.Combine(Path.GetTempPath(), "orion-world-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(worldsRoot, "default"));

        var config = new OrionConfig
        {
            Server = new ServerSection
            {
                Orion = new OrionSection { SpawnWorldIdentifier = "default", TicksPerSecond = 20 },
                WorldDefaultSettings = new WorldProperties
                {
                    Identifier = "default",
                    Dimensions = [new DimensionConfig { Identifier = "overworld" }]
                }
            }
        };

        WorldHost host = WorldHost.Bootstrap(config, worldsRoot, startScheduler: false);
        host.Dispose();

        Assert.False(OrionInfo.CanAcceptPlayers);
        OrionInfo.SetCanAcceptPlayers(true);
    }
}
