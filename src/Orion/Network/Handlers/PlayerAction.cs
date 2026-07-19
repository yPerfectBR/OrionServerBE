namespace Orion.Network.Handlers;

using Orion.Protocol.Enums;
using Orion;
using Orion.Protocol.Packets;
using Orion.RakNet;
using Orion.Player.Traits;


public static class PlayerAction
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        PlayerActionPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (PlayerActionPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            return;
        }

        if (packet.ActionType == PlayerActionType.ChangeDimensionAck)
        {
            PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
            chunkRendering?.ForceReloadViewDistance();
            player.FlushClientWorldStateSyncIfPending(force: true);
            return;
        }

        player.LastActionFace = packet.BlockFace;
        player.LastActionBlockPosition = packet.BlockPosition;
        player.LastActionResultPosition = packet.ResultPosition;
    }
}










