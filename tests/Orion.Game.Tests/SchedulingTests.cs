using Orion.Config;
using Orion.Scheduling;
using Orion.World.Threading;

namespace Orion.Game.Tests;

public sealed class SchedulingTests
{
    [Fact]
    public void PacketIngress_TreatsLoginAsGlobalPacket()
    {
        Assert.True(PacketIngress.IsGlobalPacket(Orion.Protocol.Enums.PacketId.Login));
        Assert.True(PacketIngress.IsGlobalPacket(Orion.Protocol.Enums.PacketId.RequestNetworkSettings));
        Assert.False(PacketIngress.IsGlobalPacket(Orion.Protocol.Enums.PacketId.PlayerAuthInput));
    }

    [Fact]
    public void AreaShard_IsolatesChunksBetweenAreas()
    {
        AreaShardManager manager = new([
            new ThreadingAreaConfig { Name = "city", Start = [0, 0], End = [2, 2] }
        ]);

        AreaShard city = manager.ResolveShard(1, 1);
        AreaShard outside = manager.ResolveShard(10, 10);

        city.SetChunk(new Orion.World.Chunk.Chunk(1, 1, Orion.Protocol.Enums.DimensionType.Overworld));
        outside.SetChunk(new Orion.World.Chunk.Chunk(10, 10, Orion.Protocol.Enums.DimensionType.Overworld));

        Assert.Equal(1, city.ChunkCount);
        Assert.Equal(1, outside.ChunkCount);
        Assert.NotEqual(city.AreaIndex, outside.AreaIndex);
    }

    [Fact]
    public void ServerHost_Bootstrap_EnablesPlayerAcceptance()
    {
        string worldsRoot = Path.Combine(Path.GetTempPath(), "orion-game-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(worldsRoot, "default"));

        var config = new OrionConfig
        {
            Server = new ServerSection
            {
                Orion = new OrionSection { SpawnWorldIdentifier = "default", TicksPerSecond = 20, OfflineMode = true },
                WorldDefaultSettings = new WorldProperties
                {
                    Identifier = "default",
                    Dimensions =
                    [
                        new DimensionConfig
                        {
                            Identifier = "overworld",
                            ThreadingAreas =
                            [
                                new ThreadingAreaConfig { Name = "spawn", Start = [0, 0], End = [1, 1] }
                            ],
                            ChunkPregeneration =
                            [
                                new ChunkPregenerationConfig { Start = [0, 0], End = [0, 0], MemoryLock = false }
                            ]
                        }
                    ]
                }
            }
        };

        try
        {
            using ServerHost host = ServerHost.Bootstrap(config, worldsRoot, startSchedulers: false);
            Assert.True(OrionInfo.CanAcceptPlayers);
            Assert.NotNull(host.World.GetDimension("overworld"));
            Assert.True(host.Server.Properties.AreaThreadingEnabled);
        }
        finally
        {
            OrionInfo.SetCanAcceptPlayers(true);
        }
    }
}
