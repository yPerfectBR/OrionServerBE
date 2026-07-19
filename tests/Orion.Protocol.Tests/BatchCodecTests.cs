using Orion.Protocol.Enums;
using Orion.Protocol.Io;
using Orion.Protocol.Packets;

namespace Orion.Protocol.Tests;

public sealed class BatchCodecTests
{
    [Fact]
    public void DecodePackets_SplitsMultiplePacketBodies()
    {
        byte[] login = PacketCodec.SerializeToBytes(new LoginPacket
        {
            Protocol = 1001,
            Identity = "a",
            Client = "b"
        });
        byte[] status = PacketCodec.SerializeToBytes(new PlayStatusPacket(PlayStatus.LoginSuccess));

        byte[] batch = new byte[8192];
        int written = BatchCodec.EncodePackets([login, status], batch);
        ReadOnlyMemory<byte>[] decoded = BatchCodec.DecodePackets(batch.AsMemory(0, written));

        Assert.Equal(2, decoded.Length);
        Assert.Equal(login, decoded[0].ToArray());
        Assert.Equal(status, decoded[1].ToArray());
    }

    [Fact]
    public void DecodePacketObjects_RestoresTypedPackets()
    {
        byte[] settings = PacketCodec.SerializeToBytes(new NetworkSettingsPacket
        {
            CompressionThreshold = 1,
            CompressionMethod = CompressionMethod.Zlib
        });

        byte[] batch = new byte[4096];
        int written = BatchCodec.EncodePackets([settings], batch);
        List<DataPacket> packets = BatchCodec.DecodePacketObjects(batch.AsMemory(0, written));

        Assert.Single(packets);
        NetworkSettingsPacket decoded = Assert.IsType<NetworkSettingsPacket>(packets[0]);
        Assert.Equal(1, decoded.CompressionThreshold);
        Assert.Equal(CompressionMethod.Zlib, decoded.CompressionMethod);
    }
}
