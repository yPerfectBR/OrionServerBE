using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;

namespace Orion.Protocol.Packets;

[Packet(PacketId.ServerToClientHandshake)]
public sealed record ServerToClientHandshakePacket : DataPacket
{
    public byte[] Jwt = [];

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Jwt.Length);
        writer.WriteBytes(Jwt);
    }

    public override void Deserialize(BinaryReader reader)
    {
        int length = checked((int)reader.ReadVarUInt());
        Jwt = reader.ReadBytes(length).ToArray();
    }
}
