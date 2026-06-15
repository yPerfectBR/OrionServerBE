namespace Orion.Network.Handlers;

using Orion;
using Orion.Protocol.Packets;
using Orion.RakNet;


public static class ClientCacheStatus
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ClientCacheStatusPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ClientCacheStatusPacket)Protocol.Io.Packet.Deserialize(reader);
    }
}










