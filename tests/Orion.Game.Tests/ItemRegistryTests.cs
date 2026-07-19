using Xunit;
using Orion.Item;

namespace Orion.Game.Tests;

public sealed class ItemRegistryTests
{
    [Fact]
    public void EnsureLoaded_registers_curated_grass_block()
    {
        ItemRegistry.EnsureLoaded();
        ItemType? grass = ItemType.Get("minecraft:grass_block");
        Assert.NotNull(grass);
        Assert.True(grass!.NetworkId > 0);
        Assert.Contains("minecraft:grass_block", ItemRegistry.GetGiveableIdentifiers());
    }
}
