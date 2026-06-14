using Orion.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Packets;
using ProtoAttribute = Orion.Protocol.Types.Attribute;

namespace Orion.Protocol.Packets;

[Packet(PacketId.UpdateAttributes)]
public sealed record UpdateAttributesPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong RuntimeId;

    /// <summary>
    /// Attribute values to update.
    /// </summary>
    public List<ProtoAttribute> Attributes = [];

    /// <summary>
    /// Server tick for this update.
    /// </summary>
    public ulong Tick;

    public override void Deserialize(BinaryReader reader)
    {
        RuntimeId = reader.ReadVarULong();
        Attributes = ProtoAttribute.ReadList(reader);
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(RuntimeId);
        ProtoAttribute.WriteList(writer, Attributes);
        writer.WriteVarULong(Tick);
    }
}
