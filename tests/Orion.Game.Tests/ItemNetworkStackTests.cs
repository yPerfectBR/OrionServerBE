using Orion.Block;
using Orion.Item;
using Orion.Protocol.Registry;
using Orion.Protocol.Types;

namespace Orion.Game.Tests;

public sealed class ItemNetworkStackTests
{
    [Fact]
    public void Dirt_ToNetworkStack_UsesCuratedNetworkIdAndBlockHash()
    {
        ItemRegistry.EnsureLoaded();
        BlockRegistry.EnsureLoaded();

        ItemType dirt = ItemType.Get("minecraft:dirt")!;
        LegacyItem stack = ItemType.ToNetworkStack(dirt);

        Assert.Equal(3, stack.NetworkId);
        Assert.Equal(BedrockBlockStates.Dirt, stack.NetworkBlockId);
    }

    [Fact]
    public void GrassBlock_ToNetworkStack_UsesCuratedNetworkIdAndBlockHash()
    {
        ItemRegistry.EnsureLoaded();
        BlockRegistry.EnsureLoaded();

        ItemType grass = ItemType.Get("minecraft:grass_block")!;
        LegacyItem stack = ItemType.ToNetworkStack(grass);

        Assert.Equal(2, stack.NetworkId);
        Assert.Equal(BedrockBlockStates.GrassBlock, stack.NetworkBlockId);
    }

    [Fact]
    public void CreativeItem_LookupUsesCreativeIndex_NotNetworkId()
    {
        ItemRegistry.EnsureLoaded();

        ItemStack? grass = ItemType.GetCreativeItem(0);
        ItemStack? dirt = ItemType.GetCreativeItem(1);

        Assert.NotNull(grass);
        Assert.NotNull(dirt);
        Assert.Equal("minecraft:grass_block", grass!.Identifier);
        Assert.Equal("minecraft:dirt", dirt!.Identifier);
    }
}
