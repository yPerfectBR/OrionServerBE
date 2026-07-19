using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

[Packet(PacketId.ActorEvent)]
public sealed record ActorEventPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong ActorRuntimeId;

    /// <summary>
    /// Actor event type.
    /// </summary>
    public ActorEvent Event;

    /// <summary>
    /// Event-specific data value.
    /// </summary>
    public int Data;
    public Optional<Vec3f> FiredAt = new();

    public override void Deserialize(BinaryReader reader)
    {
        ActorRuntimeId = reader.ReadVarULong();
        Event = (ActorEvent)reader.ReadUInt8();
        Data = reader.ReadZigZag();
        FiredAt.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(ActorRuntimeId);
        writer.WriteUInt8((byte)Event);
        writer.WriteZigZag(Data);
        FiredAt.Write(writer);
    }
}
