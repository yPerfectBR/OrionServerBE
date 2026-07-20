using Orion.Containers;
using Orion.Item;
using OrionContainers;
using OrionInventory;

namespace Orion.Game.Tests;

public sealed class InventoryTests
{
    [Fact]
    public void EntityInventory_AddItem_StoresRegisteredBlockItem()
    {
        ItemRegistry.EnsureLoaded();
        ItemType? dirt = ItemType.Get("minecraft:dirt");
        Assert.NotNull(dirt);

        Orion.Entity.Entity entity = new("minecraft:zombie");
        EntityInventoryTrait inventory = new(entity);

        bool added = inventory.Container.AddItem(new ItemStack(dirt!, 16));
        Assert.True(added);
        Assert.Equal(16, (int)inventory.Container.GetItem(0)!.StackSize);
    }

    [Fact]
    public void Container_AddItem_StacksMatchingItemsBeforeUsingNewSlot()
    {
        ItemRegistry.EnsureLoaded();
        ItemType? dirt = ItemType.Get("minecraft:dirt");
        Assert.NotNull(dirt);

        Container container = new(ContainerType.Inventory, 36);
        Assert.True(container.AddItem(new ItemStack(dirt!, 32)));
        Assert.True(container.AddItem(new ItemStack(dirt!, 16)));

        Assert.Equal(48, (int)container.GetItem(0)!.StackSize);
        Assert.Null(container.GetItem(1));
    }
}
