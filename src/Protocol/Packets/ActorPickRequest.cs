using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;

namespace Orion.Protocol.Packets;

[Packet(PacketId.ActorPickRequest)]
public sealed record ActorPickRequestPacket : DataPacket
{
    /// <summary>
    /// Unique id of the picked actor.
    /// </summary>
    public long EntityUniqueId;

    /// <summary>
    /// Hotbar slot used for the pick request.
    /// </summary>
    public byte HotBarSlot;

    /// <summary>
    /// Whether metadata is requested.
    /// </summary>
    public bool WithData;

    public override void Deserialize(BinaryReader reader)
    {
        EntityUniqueId = reader.ReadInt64(true);
        HotBarSlot = reader.ReadUInt8();
        WithData = reader.ReadBool();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt64(EntityUniqueId, true);
        writer.WriteUInt8(HotBarSlot);
        writer.WriteBool(WithData);
    }
}
