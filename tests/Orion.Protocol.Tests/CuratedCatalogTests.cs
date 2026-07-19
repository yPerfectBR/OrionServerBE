using Orion.Protocol.Packets;
using Orion.Protocol.Registry;
using Orion.Protocol.Types;

namespace Orion.Protocol.Tests;

[Collection(nameof(CuratedCatalogCollection))]
public sealed class CuratedCatalogTests
{
    public CuratedCatalogTests()
    {
        CuratedItemCatalog.ResetForTests();
    }

    [Fact]
    public void ItemRegistry_ContainsVanillaPalette()
    {
        byte[] payload = CuratedItemCatalog.GetItemRegistryPayload();
        Assert.NotEmpty(payload);
        Assert.Contains("vanilla", CuratedItemCatalog.Source, StringComparison.Ordinal);

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        ItemRegistryPacket packet = new();
        packet.Deserialize(reader);

        Assert.True(packet.Items.Count > 1000, $"expected full palette, got {packet.Items.Count}");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:grass_block");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:dirt");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:bedrock");
        Assert.Contains(packet.Items, i => i.Name == "minecraft:air");
    }

    [Fact]
    public void CreativeContent_DefaultsToNatureOnly_WithoutPlugins()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        Assert.Equal(4, packet.Groups.Count);
        Assert.Equal(1, packet.Groups[0].Category);
        Assert.Equal(2, packet.Groups[1].Category);
        Assert.Equal(3, packet.Groups[2].Category);
        Assert.Equal(4, packet.Groups[3].Category);
        Assert.All(packet.Groups, g => Assert.Equal(string.Empty, g.Name));
        Assert.All(packet.Groups, g => Assert.Equal(0, g.Icon.NetworkId));

        Assert.Equal(3, packet.Items.Count);
        Assert.Equal(
            new[] { 2, 3, 7 },
            packet.Items.Select(i => i.ItemInstance.NetworkId).ToArray());
        Assert.All(packet.Items, i => Assert.Equal(1u, i.GroupIndex));

        Assert.Equal(
            ["minecraft:grass_block", "minecraft:dirt", "minecraft:bedrock"],
            CuratedItemCatalog.GetCreativeMenuItems().Select(i => i.Identifier).ToArray());
        Assert.Empty(CuratedItemCatalog.GetLoadedCreativePlugins());
        Assert.True(CuratedItemCatalog.NonNatureCreativeTabsEmpty);
    }

    [Fact]
    public void CreativeContent_IncludesRegisteredPluginTabItems()
    {
        CuratedItemCatalog.RegisterCreativeTabEntries(
            "MinimalInventoryItems",
            (1, "minecraft:cobblestone"),
            (3, "minecraft:wooden_sword"),
            (4, "minecraft:stick"));

        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        Assert.Equal(6, packet.Items.Count);
        Assert.Equal(
            new[] { 4, 2, 3, 7, 339, 352 },
            packet.Items.Select(i => i.ItemInstance.NetworkId).ToArray());
        Assert.Equal(
            new uint[] { 0, 1, 1, 1, 2, 3 },
            packet.Items.Select(i => i.GroupIndex).ToArray());

        Assert.Equal(
            [
                "minecraft:cobblestone",
                "minecraft:grass_block",
                "minecraft:dirt",
                "minecraft:bedrock",
                "minecraft:wooden_sword",
                "minecraft:stick"
            ],
            CuratedItemCatalog.GetCreativeMenuItems().Select(i => i.Identifier).ToArray());
        Assert.Contains("MinimalInventoryItems", CuratedItemCatalog.GetLoadedCreativePlugins());
        Assert.False(CuratedItemCatalog.NonNatureCreativeTabsEmpty);
    }

    [Fact]
    public void Allowlist_MatchesOrionItemsJson_WithoutPlugins()
    {
        CuratedItemCatalog.GetItemRegistryPayload();
        IReadOnlyCollection<string> allowlist = CuratedItemCatalog.GetAllowlistedIdentifiers();

        Assert.Equal(5, allowlist.Count);
        Assert.Contains("minecraft:grass_block", allowlist);
        Assert.Contains("minecraft:barrier", allowlist);
        Assert.DoesNotContain("minecraft:cobblestone", allowlist);
        Assert.DoesNotContain("minecraft:diamond", allowlist);
    }

    [Fact]
    public void CreativeContent_GrassDirtBedrockHaveCorrectBlockHashes()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        CreativeItem grass = Assert.Single(packet.Items, i => i.ItemInstance.NetworkId == 2);
        CreativeItem dirt = Assert.Single(packet.Items, i => i.ItemInstance.NetworkId == 3);
        CreativeItem bedrock = Assert.Single(packet.Items, i => i.ItemInstance.NetworkId == 7);

        Assert.Equal(BedrockBlockStates.GrassBlock, grass.ItemInstance.NetworkBlockId);
        Assert.Equal(BedrockBlockStates.Dirt, dirt.ItemInstance.NetworkBlockId);
        Assert.Equal(BedrockBlockStates.Bedrock, bedrock.ItemInstance.NetworkBlockId);
        Assert.Null(grass.ItemInstance.RawData);
        Assert.Null(dirt.ItemInstance.RawData);
        Assert.Null(bedrock.ItemInstance.RawData);
    }

    [Fact]
    public void CreativeContent_ItemDescriptorsIncludeEmptyUserDataTrailer()
    {
        byte[] payload = CuratedItemCatalog.GetCreativeContentPayload();

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        CreativeContentPacket packet = new();
        packet.Deserialize(reader);

        CreativeItem grass = Assert.Single(packet.Items, i => i.ItemInstance.NetworkId == 2);
        byte[] grassDescriptor = SerializeDescriptor(grass.ItemInstance);
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
    public void BedrockBlockStates_MatchVanillaHashes()
    {
        CuratedItemCatalog.GetItemRegistryPayload();

        Assert.True(CuratedItemCatalog.TryGetByIdentifier("minecraft:bedrock", out CuratedItem bedrock));
        Assert.Equal(BedrockBlockStates.Bedrock, bedrock.BlockStateHash);
        Assert.True(CuratedItemCatalog.TryGetByIdentifier("minecraft:grass_block", out CuratedItem grass));
        Assert.Equal(BedrockBlockStates.GrassBlock, grass.BlockStateHash);
    }
}

[CollectionDefinition(nameof(CuratedCatalogCollection), DisableParallelization = true)]
public sealed class CuratedCatalogCollection;
