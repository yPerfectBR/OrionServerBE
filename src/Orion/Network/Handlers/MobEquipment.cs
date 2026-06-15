namespace Orion.Network.Handlers;

using Orion;
using Orion.Entity.Traits;
using Orion.Protocol.Packets;
using Orion.RakNet;

public static class MobEquipment
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        MobEquipmentPacket packet = (MobEquipmentPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            return;
        }

        if (packet.EntityRuntimeId != 0 && packet.EntityRuntimeId != player.RuntimeId)
        {
            return;
        }

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return;
        }

        if (packet.HotBarSlot < 9)
        {
            inventory.SetHeldItem(packet.HotBarSlot);
        }
    }
}
