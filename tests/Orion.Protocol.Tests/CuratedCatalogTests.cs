using Orion.Protocol.Packets;
using Orion.Protocol.Registry;

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
    public void CreativeContent_ContainsExactlyFiveBlocks()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        Assert.Single(packet.Groups);
        Assert.Equal("orion.blocks", packet.Groups[0].Name);
        Assert.Equal(5, packet.Items.Count);
        Assert.All(packet.Items, item => Assert.Equal(0, item.GroupIndex));
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
