namespace Orion.Network.Handlers;

using Orion;
using Orion.Entity.Traits;
using Orion.Gameplay;
using Orion.Player;
using Orion.Player.Traits;
using Orion.Plugins;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.RakNet;
using Orion.Scheduling;
using Orion.Entity.Traits.Types;
using Orion.World;
using Orion.World.Coordinates;
using Dimension = Orion.World.Dimension;


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

        PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (chunkRendering is not null)
        {
            chunkRendering.StartChunkLoad();
        }

        DebugTrait? debugTrait = player.GetTrait<DebugTrait>();
        if (debugTrait is null)
        {
            debugTrait = player.AddTrait(new DebugTrait(player));
            debugTrait.OnSpawn(new EntitySpawnOptions(InitialSpawn: false));
        }

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










