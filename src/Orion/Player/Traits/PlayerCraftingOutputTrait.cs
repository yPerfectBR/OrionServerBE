namespace Orion.Player.Traits;

using Orion.Entity.Container;
using Orion.Protocol.Enums;
using Orion.Protocol.Nbt;
using Orion.Containers;
using Orion.Player;

using Entity = Orion.Entity.Entity;
using Orion.Entity.Traits.Types;
using Orion.Entity.Traits;

/// <summary>
/// Virtual 1-slot container used for crafting and creative item output.
/// </summary>
public sealed class PlayerCraftingOutputTrait : PlayerTrait
{
    public new static string Identifier => "crafting_output";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    public EntityContainer Container { get; }

    public PlayerCraftingOutputTrait(Entity entity) : base(entity)
    {
        Container = new EntityContainer(Player, ContainerType.Inventory, 1);
    }

    public override EntityTrait Clone(Entity entity)
    {
        PlayerCraftingOutputTrait clone = new(entity);
        if (Container.GetItem(0) is { } item)
        {
            clone.Container.SetItem(0, item);
        }

        return clone;
    }

    public override void OnRead(CompoundTag tag)
    {
        CompoundTag? containerTag = tag.Get<CompoundTag>("container");
        if (containerTag is null)
        {
            return;
        }

        Container.Deserialize(containerTag);
    }

    public override void OnWrite(CompoundTag tag)
    {
        tag.Set("container", Container.Serialize());
    }
}
