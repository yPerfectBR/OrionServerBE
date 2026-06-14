using Orion.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Packets;

namespace Orion.Protocol.Packets;

[Packet(PacketId.SetLocalPlayerAsInitialized)]
public sealed record SetLocalPlayerAsInitializedPacket : DataPacket
{   
    /// <summary>
    /// The runtime id of the entity
    /// </summary>
    public ulong EntityRuntimeId;

    public override void Deserialize(BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
    }
}
