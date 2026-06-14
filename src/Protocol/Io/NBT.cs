using Orion.Protocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Io;

/// <summary>Compatibility alias for Basalt Io.NBT references.</summary>
public static class NBT
{
    public static T ReadTag<T>(BinaryReader reader, TagOptions options = default) where T : BaseTag =>
        NbtCodec.ReadTag<T>(reader, options);

    public static BaseTag ReadTag(BinaryReader reader, TagOptions options = default) =>
        NbtCodec.ReadTag(reader, options);

    public static BaseTag ReadTag(BinaryReader reader, TagType type, TagOptions options = default) =>
        NbtCodec.ReadTag(reader, type, options);

    public static void WriteTag(BinaryWriter writer, BaseTag tag, TagOptions options = default) =>
        NbtCodec.WriteTag(writer, tag, options);
}
