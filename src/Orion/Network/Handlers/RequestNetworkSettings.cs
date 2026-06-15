namespace Orion.Network.Handlers;

using Orion;
using Orion.Protocol;
using Orion.Protocol.Enums;
using Orion.Protocol.Io;
using Orion.Protocol.Packets;
using Orion.RakNet;


public static class RequestNetworkSettings
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestNetworkSettingsPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (RequestNetworkSettingsPacket)Protocol.Io.Packet.Deserialize(reader);

        if (packet.Protocol != Constants.ProtocolVersion)
        {
            DisconnectReason reason = packet.Protocol < Constants.ProtocolVersion
                ? DisconnectReason.OutdatedClient
                : DisconnectReason.OutdatedServer;

            DisconnectPacket disconnect = new()
            {
                Reason = reason,
                HideDisconnectionScreen = true,
                Message = "",
                FilteredMessage = ""
            };

            server.Network.SendPacket(connection, disconnect, CompressionMethod.NotPresent);
            return;
        }

        NetworkSettingsPacket response = new()
        {
            CompressionThreshold = (ushort)Math.Clamp(server.Properties.CompressionThreshold, 0, ushort.MaxValue),
            CompressionMethod = server.Properties.CompressionMethod,
            ClientThrottle = false,
            ClientThrottleThreshold = 0,
            ClientThrottleScalar = 0f
        };

        server.Network.SendPacket(connection, response, CompressionMethod.NotPresent);
    }
}










