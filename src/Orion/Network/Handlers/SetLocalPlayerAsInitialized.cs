namespace Orion.Network.Handlers;

using Orion;
using Orion.Api;
using Orion.Api.Math;
using Orion.Gameplay;
using Orion.Player;
using Orion.Plugins;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.RakNet;
using Orion.Scheduling;
using Orion.World;
using Dimension = Orion.World.Dimension;
using HudVisibility = Orion.Protocol.Enums.HudVisibility;
using HudElement = Orion.Protocol.Enums.HudElement;


public static class SetLocalPlayerAsInitialized
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        SetLocalPlayerAsInitializedPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (SetLocalPlayerAsInitializedPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            Warn("SetLocalPlayerAsInitialized received for unknown player session.");
            return;
        }
        ulong tick = player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0;

        player.Session?.Send(player.CreateActorDataPacket(tick));
        player.SendAttributes();

        player.GetTrait<IPlayerChunkView>()?.StartChunkLoad();

        player.SetSpawned(true);

        // Minimal engine: hide gameplay HUD until opt-in plugins re-enable their pieces.
        player.SetHud(
            HudVisibility.Hide,
            HudElement.HotBar,
            HudElement.Health,
            HudElement.Hunger);

        if (PluginHost.Services.TryGet(out IPlayerInventoryService? inventory) && inventory is not null)
        {
            _ = inventory.TrySyncToClient(player);
            inventory.EnableHud(player);
        }

        if (PluginHost.Services.TryGet(out IAttributesApi? attributes) && attributes is not null)
        {
            attributes.EnableHud(player);
        }

        string joinMessage = $"§e{player.Username} joined the server.";
        foreach (PlayerSession targetSession in server.Sessions.Values)
        {
            if (ReferenceEquals(targetSession.ActiveEntity, player))
            {
                continue;
            }

            targetSession.SendMessage(joinMessage);
        }

        Info($"Player {player.Username} has spawned.");
    }
}










