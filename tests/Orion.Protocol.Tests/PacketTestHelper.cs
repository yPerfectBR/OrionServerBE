using Orion.Protocol.Io;
using Orion.Protocol.Packets;

namespace Orion.Protocol.Tests;

internal static class PacketTestHelper
{
    public static T RoundTrip<T>(T packet) where T : DataPacket
    {
        byte[] bytes = PacketCodec.SerializeToBytes(packet);
        DataPacket decoded = PacketCodec.DeserializeFromBytes(bytes);
        Assert.IsType<T>(decoded);
        return (T)decoded;
    }
}
