namespace Orion.Network.Handlers;

using Orion;
using Orion.Api;
using Orion.Api.Math;
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
        int maxViewDistance = Math.Clamp(server.Properties.MaxViewDistance, 4, ChunkViewMath.MaxBedrockViewDistance);
        int clientMax = packet.MaxChunkRadius > 0
            ? packet.MaxChunkRadius
            : ChunkViewMath.MaxBedrockViewDistance;

        // Cap the Chebyshev stream so SquareToCircle(stream) still fits in clientMax.
        // Clamping only ChunkRadiusUpdated to clientMax (while streaming a larger square)
        // makes the client cull the corners — void returns at high render distances.
        int maxChebyshev = ChunkViewMath.MaxChebyshevForClientCircle(clientMax);
        int radius = Math.Clamp(requestedRadius, 4, Math.Min(maxViewDistance, maxChebyshev));
        int bedrockRadius = ChunkViewMath.SquareToCircle(radius);

        server.Network.SendPacket(connection, new UpdateChunkRadiusPacket
        {
            ChunkRadius = bedrockRadius
        });

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            return;
        }

        player.GetTrait<IPlayerChunkView>()?.ApplyViewDistance(radius);
    }
}
