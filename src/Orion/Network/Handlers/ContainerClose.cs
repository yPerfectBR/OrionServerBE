namespace Orion.Network.Handlers;

using Orion;
using Orion.Gameplay;
using Orion.Plugins;
using Orion.Protocol.Packets;
using Orion.RakNet;

/// <summary>Fallback when VanillaInventory is not loaded / not owning the packet.</summary>
public static class ContainerClose
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ContainerClosePacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ContainerClosePacket)Protocol.Io.Packet.Deserialize(reader);

        if (SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player)
            && PluginHost.Services.TryGet(out IPlayerInventoryService? inventory)
            && inventory is not null)
        {
            _ = inventory.TryCloseInventory(player, packet.WindowId);
            player.FlushClientWorldStateSyncIfPending(force: true);
        }

        ContainerClosePacket response = new()
        {
            WindowId = packet.WindowId,
            ContainerType = packet.ContainerType,
            ServerSide = false
        };
        if (SessionLookup.TryGetSession(server, connection, out global::Orion.Player.PlayerSession? session))
        {
            session.Send(response);
        }
        else
        {
            server.Network.SendPacket(connection, response);
        }
    }
}
