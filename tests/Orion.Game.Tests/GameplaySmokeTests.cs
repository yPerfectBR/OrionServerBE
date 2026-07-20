using Orion.Block;
using Orion.Item;

namespace Orion.Game.Tests;

public sealed class GameplaySmokeTests
{
    [Fact]
    public void BlockDropHelper_ReturnsMatchingItemForGrassBlock()
    {
        ItemRegistry.EnsureLoaded();
        global::Orion.Block.Block block = new("minecraft:grass_block");
        List<ItemStack> drops = BlockDropHelper.GenerateLootFromBlock(block);
        Assert.Single(drops);
        Assert.Equal("minecraft:grass_block", drops[0].Type.Identifier);
    }

    [Fact]
    public void ItemType_LoadsCuratedBlockItem()
    {
        ItemRegistry.EnsureLoaded();
        ItemType? type = ItemType.Get("minecraft:dirt");
        Assert.NotNull(type);
        Assert.False(string.IsNullOrWhiteSpace(type!.Identifier));
    }
}
