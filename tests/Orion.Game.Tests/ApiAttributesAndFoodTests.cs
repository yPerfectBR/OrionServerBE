using Orion.Api;
using Orion.Api.Items;
using Orion.Item;
using Orion.Protocol.Nbt;
using Xunit;
using PlayerEntity = Orion.Player.Player;

namespace Orion.Game.Tests;

public sealed class ApiAttributesAndFoodTests
{
    [Fact]
    public void IEntity_SetAttribute_and_TryGetAttribute_roundtrip()
    {
        PlayerEntity player = new("attr", "0", Guid.NewGuid());
        IEntity entity = player;

        entity.SetAttribute("minecraft:health", 0f, 20f, 15f, 20f);

        Assert.True(entity.TryGetAttribute(
            "minecraft:health",
            out float min,
            out float max,
            out float current,
            out float defaultValue));
        Assert.Equal(0f, min);
        Assert.Equal(20f, max);
        Assert.Equal(15f, current);
        Assert.Equal(20f, defaultValue);
        Assert.True(player.AttributesDirty);
    }

    [Fact]
    public void IEntity_TryGetAttribute_unknown_returns_false()
    {
        PlayerEntity player = new("attr", "0", Guid.NewGuid());
        Assert.False(((IEntity)player).TryGetAttribute(
            "minecraft:not_a_real_attribute",
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void IPlayer_IsFlying_defaults_false()
    {
        PlayerEntity player = new("fly", "0", Guid.NewGuid());
        Assert.False(((IPlayer)player).IsFlying);
    }

    [Fact]
    public void IItemType_TryGetFood_reads_food_component()
    {
        ItemType.ResetForTests();
        CompoundTag food = new();
        food.Set("nutrition", new IntTag { Value = 4 });
        food.Set("saturation_modifier", new FloatTag { Value = 0.3f });
        food.Set("can_always_eat", new ByteTag { Value = 0 });
        food.Set("using_converts_to", new StringTag { Value = "minecraft:bowl" });
        CompoundTag components = new();
        components.Set("minecraft:food", food);
        CompoundTag properties = new();
        properties.Set("components", components);

        ItemType type = new(
            "minecraft:test_soup",
            networkId: 99001,
            maxStackSize: 1,
            tags: ["minecraft:is_food"],
            isComponentBased: true,
            version: 1,
            properties: properties);

        IItemType api = type;
        Assert.True(api.TryGetFood(
            out int nutrition,
            out float saturation,
            out bool canAlwaysEat,
            out string? convertsTo));
        Assert.Equal(4, nutrition);
        Assert.Equal(0.3f, saturation);
        Assert.False(canAlwaysEat);
        Assert.Equal("minecraft:bowl", convertsTo);
    }

    [Fact]
    public void IItemType_TryGetUseDurationTicks_reads_item_properties()
    {
        ItemType.ResetForTests();
        CompoundTag properties = new();
        CompoundTag components = new();
        CompoundTag itemProperties = new();
        itemProperties.Set("use_duration", new IntTag { Value = 32 });
        components.Set("item_properties", itemProperties);
        properties.Set("components", components);

        ItemType type = new(
            "minecraft:test_food_duration",
            networkId: 99002,
            maxStackSize: 64,
            tags: null,
            isComponentBased: true,
            version: 1,
            properties: properties);

        Assert.True(((IItemType)type).TryGetUseDurationTicks(out ulong ticks));
        Assert.Equal(32UL, ticks);
    }
}
