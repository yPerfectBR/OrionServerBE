using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Nbt;

namespace Orion.Protocol.Packets;

[Packet(PacketId.SyncActorProperty)]
public sealed record SyncActorPropertyPacket : DataPacket
{
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    /// <summary>
    /// Property data payload as NBT.
    /// </summary>
    public CompoundTag PropertyData = new();

    public override void Deserialize(BinaryReader reader)
    {
        PropertyData = CompoundTag.Read(reader, NetworkNbtOptions);
    }

    public override void Serialize(BinaryWriter writer)
    {
        Io.NBT.WriteTag(writer, PropertyData, NetworkNbtOptions);
    }
}
