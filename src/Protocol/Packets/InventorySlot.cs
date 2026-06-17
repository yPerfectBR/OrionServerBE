using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

[Packet(PacketId.InventorySlot)]
public sealed record InventorySlotPacket : DataPacket
{
    /// <summary>
    /// Window id of the inventory.
    /// </summary>
    public uint WindowId;

    /// <summary>
    /// Slot index in the container.
    /// </summary>
    public uint Slot;

    /// <summary>
    /// Optional full container identity.
    /// </summary>
    public Optional<FullContainerName> Container = new();

    /// <summary>
    /// Optional storage item descriptor.
    /// </summary>
    public Optional<NetworkItemStackDescriptor> StorageItem = new();

    /// <summary>
    /// New item descriptor for this slot.
    /// </summary>
    public NetworkItemStackDescriptor NewItem = new();

    public override void Deserialize(BinaryReader reader)
    {
        WindowId = reader.ReadVarUInt();
        Slot = reader.ReadVarUInt();
        Container.Read(reader);
        StorageItem.Read(reader);
        NewItem.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarUInt(WindowId);
        writer.WriteVarUInt(Slot);
        Container.Write(writer);
        StorageItem.Write(writer);
        NewItem.Write(writer);
    }
}
