using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

[Packet(PacketId.MobEquipment)]
public sealed record MobEquipmentPacket : DataPacket
{
    public ulong EntityRuntimeId;
    public NetworkItemStackDescriptor NewItem = new();
    public byte InventorySlot;
    public byte HotBarSlot;
    public byte WindowId;

    public override void Deserialize(BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
        NewItem.Read(reader);
        InventorySlot = reader.ReadUInt8();
        HotBarSlot = reader.ReadUInt8();
        WindowId = reader.ReadUInt8();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        NewItem.Write(writer);
        writer.WriteUInt8(InventorySlot);
        writer.WriteUInt8(HotBarSlot);
        writer.WriteUInt8(WindowId);
    }
}
