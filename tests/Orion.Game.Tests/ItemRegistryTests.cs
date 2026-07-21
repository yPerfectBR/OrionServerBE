using Orion.Block;
using Orion.Item;
using Orion.Protocol.Registry;

namespace Orion.Game.Tests;

[Collection("ItemCatalog")]
public sealed class ItemRegistryTests
{
    public ItemRegistryTests()
    {
        MinimalContentFixtures.RegisterAll();
    }

    [Fact]
    public void EnsureLoaded_registers_curated_grass_block()
    {
        ItemRegistry.EnsureLoaded();
        ItemType? grass = ItemType.Get("minecraft:grass_block");
        Assert.NotNull(grass);
        Assert.True(grass!.NetworkId > 0);
        Assert.Contains("minecraft:grass_block", ItemRegistry.GetGiveableIdentifiers());
    }

    [Fact]
    public void EnsureLoaded_without_plugins_has_empty_giveable_list()
    {
        ItemRegistry.ResetForTests();
        CuratedItemCatalog.ResetForTests();
        BlockRegistry.ResetForTests();

        ItemRegistry.EnsureLoaded();
        Assert.Empty(ItemRegistry.GetGiveableIdentifiers());
        Assert.Null(ItemType.Get("minecraft:grass_block"));
    }
}
