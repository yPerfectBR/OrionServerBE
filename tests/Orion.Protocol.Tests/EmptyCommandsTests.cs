using Orion.Protocol.Packets;

namespace Orion.Protocol.Tests;

public sealed class EmptyCommandsTests
{
    [Fact]
    public void AvailableCommands_RoundTrip_EmptyLists()
    {
        AvailableCommandsPacket original = new();
        AvailableCommandsPacket decoded = PacketTestHelper.RoundTrip(original);

        Assert.Empty(decoded.EnumValues);
        Assert.Empty(decoded.ChainedSubcommandValues);
        Assert.Empty(decoded.Suffixes);
        Assert.Empty(decoded.Enums);
        Assert.Empty(decoded.ChainedSubcommands);
        Assert.Empty(decoded.Commands);
        Assert.Empty(decoded.DynamicEnums);
        Assert.Empty(decoded.Constraints);
    }

    [Fact]
    public void CuratedCatalog_AvailableCommandsPayload_IsEmpty()
    {
        byte[] payload = Registry.CuratedItemCatalog.GetAvailableCommandsPayload();
        Assert.NotEmpty(payload);

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        AvailableCommandsPacket packet = new();
        packet.Deserialize(reader);

        Assert.Empty(packet.Commands);
    }
}
