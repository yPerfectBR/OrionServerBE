using Orion.Protocol.Packets;
using Orion.Protocol.Registry;
using Orion.Protocol.Types;

namespace Orion.Protocol.Tests;

public sealed class CuratedCatalogTests
{
    [Fact]
    public void ItemRegistry_ContainsExactlyFiveBlocks()
    {
        byte[] payload = CuratedItemCatalog.GetItemRegistryPayload();
        Assert.NotEmpty(payload);

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        ItemRegistryPacket packet = new();
        packet.Deserialize(reader);

        Assert.Equal(5, packet.Items.Count);
        Assert.Contains(packet.Items, i => i.Name == "minecraft:grass_block");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:dirt");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:bedrock");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:barrier");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:structure_void");
    }

    [Fact]
    public void CreativeContent_ContainsAllFiveBlocks()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        Assert.Single(packet.Groups);
        Assert.Equal("itemGroup.name.grass", packet.Groups[0].Name);
        Assert.Equal(2, packet.Groups[0].Category);
        Assert.Equal(5, packet.Items.Count);
        Assert.All(packet.Items, item => Assert.Equal(0, item.GroupIndex));
        Assert.Contains(packet.Items, item => item.ItemInstance.NetworkId == -161);
        Assert.Contains(packet.Items, item => item.ItemInstance.NetworkId == 217);
    }

    [Fact]
    public void CreativeContent_UsesEncodedInstances()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        Assert.Equal(5, packet.Items.Count);
        Assert.Equal(2, packet.Items[0].ItemInstance.NetworkId);
        Assert.Equal(3, packet.Items[1].ItemInstance.NetworkId);
        Assert.Equal(7, packet.Items[2].ItemInstance.NetworkId);
        Assert.Equal(-161, packet.Items[3].ItemInstance.NetworkId);
        Assert.Equal(217, packet.Items[4].ItemInstance.NetworkId);
        Assert.Equal(BedrockBlockStates.GrassBlock, packet.Items[0].ItemInstance.NetworkBlockId);
        Assert.Equal(BedrockBlockStates.Dirt, packet.Items[1].ItemInstance.NetworkBlockId);
        Assert.Equal(BedrockBlockStates.Bedrock, packet.Items[2].ItemInstance.NetworkBlockId);
        Assert.All(packet.Items, item => Assert.NotNull(item.ItemInstance.ExtraData));
        Assert.All(packet.Items, item => Assert.Null(item.ItemInstance.RawData));
    }

    [Fact]
    public void CreativeContent_ItemDescriptorsIncludeEmptyUserDataTrailer()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        byte[] grassDescriptor = SerializeDescriptor(packet.Items[0].ItemInstance);
        byte[] expectedTrailer = [0x0A, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        Assert.True(grassDescriptor.AsSpan(^expectedTrailer.Length).SequenceEqual(expectedTrailer));
    }

    static byte[] SerializeDescriptor(CreativeItemInstanceDescriptor descriptor)
    {
        byte[] buffer = new byte[64];
        int offset = 0;
        Basalt.Binary.BinaryWriter writer = new(buffer, ref offset);
        descriptor.Write(writer);
        return buffer[..offset];
    }

    [Fact]
    public void BedrockBlockStates_MatchCuratedHashes()
    {
        CuratedItemCatalog.GetItemRegistryPayload();

        Assert.True(CuratedItemCatalog.TryGetByIdentifier("minecraft:bedrock", out CuratedItem bedrock));
        Assert.Equal(BedrockBlockStates.Bedrock, bedrock.BlockStateHash);
        Assert.True(CuratedItemCatalog.TryGetByIdentifier("minecraft:grass_block", out CuratedItem grass));
        Assert.Equal(BedrockBlockStates.GrassBlock, grass.BlockStateHash);
    }
}
