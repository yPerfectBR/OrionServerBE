using Orion.Protocol.Io;
using Orion.Protocol.Nbt;
using Orion.Protocol.Packets;

namespace Orion.Protocol.Tests;

public sealed class NbtRoundTripTests
{
    [Fact]
    public void CompoundTag_RoundTrip_ThroughAvailableActorIdentifiers()
    {
        AvailableActorIdentifiersPacket original = new();
        original.Data.Set("test_byte", new ByteTag { Value = 7 });
        original.Data.Set("test_string", new StringTag { Value = "orion" });

        AvailableActorIdentifiersPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Equal((sbyte)7, decoded.Data.Get<ByteTag>("test_byte")?.Value);
        Assert.Equal("orion", decoded.Data.Get<StringTag>("test_string")?.Value);
    }

    [Fact]
    public void NbtCodec_ReadWriteTag_PreservesCompound()
    {
        CompoundTag original = new();
        original.Set("count", new IntTag { Value = 42 });

        byte[] buffer = new byte[256];
        int offset = 0;
        Basalt.Binary.BinaryWriter writer = new(buffer, ref offset);
        NbtCodec.WriteTag(writer, original, new TagOptions(Name: true, Type: true, VarInt: true));

        ReadOnlySpan<byte> written = buffer.AsSpan(0, offset);
        offset = 0;
        Basalt.Binary.BinaryReader reader = new(written, ref offset);
        CompoundTag decoded = NbtCodec.ReadTag<CompoundTag>(reader, new TagOptions(Name: true, Type: true, VarInt: true));

        Assert.Equal(42, decoded.Get<IntTag>("count")?.Value);
    }
}
