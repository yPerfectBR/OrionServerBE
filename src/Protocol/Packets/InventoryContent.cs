using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

[Packet(PacketId.InventoryContent)]
public sealed record InventoryContentPacket : DataPacket
{
    /// <summary>
    /// Window id of the inventory.
    /// </summary>
    public uint WindowId;

    /// <summary>
    /// Inventory content entries.
    /// </summary>
    public List<NetworkItemStackDescriptor> Content = [];

    /// <summary>
    /// Full container identity.
    /// </summary>
    public FullContainerName Container = new();

    /// <summary>
    /// Optional storage item descriptor.
    /// </summary>
    public NetworkItemStackDescriptor StorageItem = new();

    public override void Deserialize(BinaryReader reader)
    {
        WindowId = reader.ReadVarUInt();

        int count = checked((int)reader.ReadVarUInt());
        Content = new List<NetworkItemStackDescriptor>(count);
        for (int i = 0; i < count; i++)
        {
            NetworkItemStackDescriptor item = new();
            item.Read(reader);
            Content.Add(item);
        }

        Container.Read(reader);
        StorageItem.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarUInt(WindowId);
        writer.WriteVarUInt((uint)Content.Count);
        for (int i = 0; i < Content.Count; i++)
        {
            Content[i].Write(writer);
        }

        Container.Write(writer);
        StorageItem.Write(writer);
    }
}
