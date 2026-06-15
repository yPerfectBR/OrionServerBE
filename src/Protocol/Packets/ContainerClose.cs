using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;

namespace Orion.Protocol.Packets;

[Packet(PacketId.ContainerClose)]
public sealed record ContainerClosePacket : DataPacket
{
    /// <summary>
    /// Window id of the container.
    /// </summary>
    public byte WindowId;

    /// <summary>
    /// Container type id.
    /// </summary>
    public byte ContainerType;

    /// <summary>
    /// Whether this close is server initiated.
    /// </summary>
    public bool ServerSide;

    public override void Deserialize(BinaryReader reader)
    {
        WindowId = reader.ReadUInt8();
        ContainerType = reader.ReadUInt8();
        ServerSide = reader.ReadBool();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteUInt8(WindowId);
        writer.WriteUInt8(ContainerType);
        writer.WriteBool(ServerSide);
    }
}
