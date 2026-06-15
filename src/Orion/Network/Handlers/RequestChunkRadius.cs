namespace Orion.Network.Handlers;

using Orion;
using Orion.Player.Traits;
using Orion.Protocol.Packets;
using Orion.RakNet;


public static class RequestChunkRadius
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestChunkRadiusPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (RequestChunkRadiusPacket)Protocol.Io.Packet.Deserialize(reader);

        int requestedRadius = packet.ChunkRadius;
        int maxViewDistance = Math.Clamp(server.Properties.MaxViewDistance, 4, 120);
        int radius = Math.Clamp(requestedRadius, 4, maxViewDistance);
        // UpdateChunkRadiusPacket response = new()
        // {
        //     ChunkRadius = radius
        // };  

        // THIS STUPID PACKET CRASHES MOBILE DEVICES!!!
        /// PLEASE KEEP IT COMMENTED OUT!
        // server.Network.SendPacket(connection, response);

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            return;
        }

        PlayerChunkRenderingTrait? chunkRendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (chunkRendering is null)
        {
            return;
        }

        chunkRendering.ApplyViewDistance(radius);
    }
}










