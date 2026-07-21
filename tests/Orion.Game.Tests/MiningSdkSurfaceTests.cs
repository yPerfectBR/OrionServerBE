using Orion.Api;
using Orion.Api.Blocks;
using Orion.Api.Events;
using Orion.Api.Items;
using Orion.Api.Math;
using Orion.Block;
using Orion.Item;
using Orion.Protocol.Registry;
using PlayerEntity = Orion.Player.Player;

namespace Orion.Game.Tests;

[Collection("ItemCatalog")]
public sealed class MiningSdkSurfaceTests
{
    public MiningSdkSurfaceTests()
    {
        MinimalContentFixtures.RegisterAll();
        ItemRegistry.EnsureLoaded();
        BlockRegistry.EnsureLoaded();
    }

    [Fact]
    public void Blocks_Create_Air_ExposesHardnessAndNetworkId()
    {
        IBlock air = Blocks.Create("minecraft:air");
        Assert.Equal("minecraft:air", air.Type.Identifier);
        Assert.True(air.Type.Air);
        Assert.False(air.Type.Solid);
        Assert.Equal(0f, air.Type.Hardness);
        Assert.Equal(BedrockBlockStates.Air, air.Permutation.NetworkId);
    }

    [Fact]
    public void IServer_Emit_PlayerBreakBlock_Cancel_PreventsApply()
    {
        IServer server = new Server();
        PlayerEntity player = new("miner", "0", Guid.NewGuid());
        bool applied = false;

        ((Server)server).On<PlayerBreakBlockSignal>(
            ServerEvent.PlayerBreakBlock,
            s => s.Cancel(),
            EventPriority.High);

        PlayerBreakBlockSignal signal = new(player, new BlockPos(0, 100, 0), blockFace: 1);
        server.Emit(signal);
        if (signal.Emit())
        {
            applied = true;
        }

        Assert.True(signal.Cancelled);
        Assert.False(applied);
    }

    [Fact]
    public void ItemType_ExposesTagsOnApi()
    {
        IItemType? dirt = ItemType.Get("minecraft:dirt");
        Assert.NotNull(dirt);
        Assert.NotNull(dirt!.Tags);
    }
}
