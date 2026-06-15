using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;

namespace Orion.Protocol.Packets;

[Packet(PacketId.RemoveActor)]
public sealed record RemoveActorPacket : DataPacket
{
    /// <summary>
    /// Unique id of the actor to remove.
    /// </summary>
    public long EntityUniqueId;

    public override void Deserialize(BinaryReader reader)
    {
        EntityUniqueId = reader.ReadVarLong();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarLong(EntityUniqueId);
    }
}
