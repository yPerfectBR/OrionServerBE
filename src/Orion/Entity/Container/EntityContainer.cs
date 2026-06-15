namespace Orion.Entity.Container;

using Orion.Containers;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;

public sealed class EntityContainer : Containers.Container
{
    public Entity Entity { get; }

    public EntityContainer(Entity entity, ContainerType type, int size) : base(type, size)
    {
        Entity = entity;
    }

    public bool IsOwnedBy(Orion.Player.Player player)
    {
        return ReferenceEquals(Entity, player);
    }

    public override void SetItem(int slot, Orion.Item.ItemStack item)
    {
        base.SetItem(slot, item);
    }

    public override void UpdateSlot(int slot)
    {
        Entity.OnContainerUpdate(this);
        if (slot < 0 || slot >= GetSize())
        {
            base.UpdateSlot(slot);
            return;
        }

        if (Entity is Orion.Player.Player player && Identifier == 0 && player.Spawned)
        {
            player.Send(new InventorySlotPacket
            {
                WindowId = Identifier ?? 0,
                Slot = slot,
                Container = new Optional<FullContainerName>
                {
                    HasValue = true,
                    Value = new FullContainerName { ContainerId = (byte)ContainerId.Inventory }
                },
                NewItem = ToItemInstanceNew(GetItem(slot))
            });
        }

        base.UpdateSlot(slot);
    }

    public override void Update()
    {
        Entity.OnContainerUpdate(this);
        base.Update();
    }

    protected override long GetContainerEntityUniqueId()
    {
        return Entity.UniqueId;
    }

    protected override Orion.Protocol.Types.BlockPos GetContainerPosition()
    {
        if (Entity is Orion.Player.Player)
        {
            return new Orion.Protocol.Types.BlockPos
            {
                X = 0,
                Y = 0,
                Z = 0
            };
        }

        return new Orion.Protocol.Types.BlockPos
        {
            X = (int)MathF.Floor(Entity.Position.X),
            Y = (int)MathF.Floor(Entity.Position.Y),
            Z = (int)MathF.Floor(Entity.Position.Z)
        };
    }

    // TODO: Add proper checks, e.g container already opened
    // Or if something is preventing it from opening
    protected override bool CanOpen(Orion.Player.Player player, int windowId)
    {
        return true;
    }

    protected override byte GetFullContainerNameId()
    {
        if (Identifier == 124)
        {
            return (byte)ContainerName.Cursor;
        }

        return base.GetFullContainerNameId();
    }
}






