using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;

namespace Orion.Protocol.Packets;

[Packet(PacketId.ClientToServerHandshake)]
public sealed record ClientToServerHandshakePacket : DataPacket
{
    public override void Serialize(BinaryWriter writer)
    {
    }

    public override void Deserialize(BinaryReader reader)
    {
    }
}
