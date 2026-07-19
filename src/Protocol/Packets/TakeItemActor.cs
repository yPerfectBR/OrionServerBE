using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;

namespace Orion.Protocol.Packets;

[Packet(PacketId.TakeItemActor)]
public sealed record TakeItemActorPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the item actor.
    /// </summary>
    public ulong ItemEntityRuntimeId;

    /// <summary>
    /// Runtime id of the actor taking the item.
    /// </summary>
    public ulong TakerEntityRuntimeId;

    public override void Deserialize(BinaryReader reader)
    {
        ItemEntityRuntimeId = reader.ReadVarULong();
        TakerEntityRuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(ItemEntityRuntimeId);
        writer.WriteVarULong(TakerEntityRuntimeId);
    }
}
