using Orion.Entity.Traits;
using Orion.Gameplay;
using Orion.Item;
using Orion.Item.Traits;
using Orion.Player;
using Orion.Protocol.Enums;
using Orion.Protocol.Nbt;
using Entity = Orion.Entity.Entity;

namespace VanillaAttributes;

/// <summary>
/// Implements host gameplay services for health, hunger, and food item use.
/// </summary>
public sealed class AttributeGameplayServices :
    IVanillaAttributesApi,
    IEntityHealthService,
    IPlayerHungerService,
    IPlayerItemUseHandler
{
    const ulong DefaultFoodUseTicks = 32UL;

    public IEntityHealthService Health => this;
    public IPlayerHungerService Hunger => this;

    public bool TryApplyDamage(
        Entity entity,
        float amount,
        Entity? damager = null,
        ActorDamageCause? cause = null)
    {
        EntityHealthTrait? health = entity.GetTrait<EntityHealthTrait>();
        if (health is null || !entity.IsAlive)
        {
            return false;
        }

        health.ApplyDamage(amount, damager, cause);
        return true;
    }

    public bool TryHeal(Entity entity, float amount)
    {
        EntityHealthTrait? health = entity.GetTrait<EntityHealthTrait>();
        if (health is null || amount <= 0f)
        {
            return false;
        }

        float before = health.CurrentValue;
        health.CurrentValue = MathF.Min(health.MaximumValue, health.CurrentValue + amount);
        return health.CurrentValue > before;
    }

    public bool TryGet(Entity entity, out float current, out float maximum)
    {
        EntityHealthTrait? health = entity.GetTrait<EntityHealthTrait>();
        if (health is null)
        {
            current = 0f;
            maximum = 0f;
            return false;
        }

        current = health.CurrentValue;
        maximum = health.MaximumValue;
        return true;
    }

    public bool TrySet(Entity entity, float current)
    {
        EntityHealthTrait? health = entity.GetTrait<EntityHealthTrait>();
        if (health is null)
        {
            return false;
        }

        health.CurrentValue = Math.Clamp(current, health.MinimumValue, health.MaximumValue);
        return true;
    }

    public bool TryEat(Player player, int nutrition, float saturationModifier, bool canAlwaysEat)
    {
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        return hunger is not null && hunger.Eat(nutrition, saturationModifier, canAlwaysEat);
    }

    public bool TryAddExhaustion(Player player, float amount)
    {
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (hunger is null)
        {
            return false;
        }

        hunger.Exhaustion += amount;
        return true;
    }

    public bool TryGet(Player player, out float hunger, out float saturation, out float exhaustion)
    {
        PlayerHungerTrait? trait = player.GetTrait<PlayerHungerTrait>();
        if (trait is null)
        {
            hunger = 0f;
            saturation = 0f;
            exhaustion = 0f;
            return false;
        }

        hunger = trait.CurrentValue;
        saturation = trait.Saturation;
        exhaustion = trait.Exhaustion;
        return true;
    }

    public bool TrySetHunger(Player player, float hunger, float? saturation = null)
    {
        PlayerHungerTrait? trait = player.GetTrait<PlayerHungerTrait>();
        if (trait is null)
        {
            return false;
        }

        trait.CurrentValue = Math.Clamp(hunger, trait.MinimumValue, trait.MaximumValue);
        if (saturation is float sat)
        {
            trait.Saturation = Math.Clamp(sat, 0f, trait.CurrentValue);
        }

        return true;
    }

    public bool TryBeginUse(Player player, ItemStack heldItem, out ulong durationTicks)
    {
        durationTicks = 0UL;
        ItemStackFoodTrait? food = heldItem.GetTrait<ItemStackFoodTrait>();
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (food is null || hunger is null)
        {
            return false;
        }

        if (!food.CanAlwaysEat && hunger.CurrentValue >= hunger.MaximumValue)
        {
            return false;
        }

        durationTicks = GetUseDurationTicks(heldItem);
        return true;
    }

    public bool TryCompleteUse(Player player, ItemStack heldItem, int slot)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        ItemStackFoodTrait? food = heldItem.GetTrait<ItemStackFoodTrait>();
        PlayerHungerTrait? hunger = player.GetTrait<PlayerHungerTrait>();
        if (inventory is null
            || food is null
            || hunger is null
            || !hunger.Eat(food.Nutrition, food.SaturationModifier, food.CanAlwaysEat))
        {
            return false;
        }

        heldItem.DecrementStack();
        if (heldItem.StackSize == 0)
        {
            inventory.Container.ClearSlot(slot);
        }
        else
        {
            inventory.Container.UpdateSlot(slot);
        }

        if (!string.IsNullOrWhiteSpace(food.UsingConvertsTo) && ItemType.Get(food.UsingConvertsTo) is ItemType convertedType)
        {
            ItemStack converted = new(convertedType);
            if (!inventory.Container.AddItem(converted))
            {
                _ = player.DropItem(converted);
            }
        }

        player.SendAttributes();
        return true;
    }

    static ulong GetUseDurationTicks(ItemStack item)
    {
        if (item.Type.TryGetComponentProperties("minecraft:use_duration", out CompoundTag tag))
        {
            return (ulong)Math.Max(1, tag.Get<IntTag>("value")?.Value ?? (int)DefaultFoodUseTicks);
        }

        return DefaultFoodUseTicks;
    }
}
