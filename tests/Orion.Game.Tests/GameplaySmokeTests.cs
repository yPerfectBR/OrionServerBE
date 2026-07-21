using Orion.Block;
using Orion.Item;
using Orion.Protocol.Registry;

namespace Orion.Game.Tests;

[Collection("ItemCatalog")]
public sealed class GameplaySmokeTests
{
    public GameplaySmokeTests()
    {
        MinimalContentFixtures.RegisterAll();
    }

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
