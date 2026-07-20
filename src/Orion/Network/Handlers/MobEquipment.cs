namespace Orion.Network.Handlers;

using Orion;
using Orion.Gameplay;
using Orion.Plugins;
using Orion.Protocol.Packets;
using Orion.RakNet;

/// <summary>Fallback when VanillaInventory is not loaded / not owning the packet.</summary>
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

        if (packet.HotBarSlot < 9
            && PluginHost.Services.TryGet(out IPlayerInventoryService? inventory)
            && inventory is not null)
        {
            _ = inventory.TrySetHeldSlot(player, packet.HotBarSlot);
        }
    }
}
