using Orion.Block;
using Orion.Item;
using Orion.Protocol.Registry;

namespace Orion.Game.Tests;

/// <summary>
/// Mirrors <c>orion:minimal-items</c> registrations for unit tests that boot without McMaster.
/// </summary>
internal static class MinimalContentFixtures
{
    public static void Reset()
    {
        ItemRegistry.ResetForTests();
        CuratedItemCatalog.ResetForTests();
        BlockRegistry.ResetForTests();
    }

    public static void RegisterBlocks()
    {
        BlockRegistry.RegisterPluginBlock("minecraft:air", BedrockBlockStates.Air, solid: false, air: true, hardness: 0f);
        BlockRegistry.RegisterPluginBlock(
            "minecraft:structure_void", BedrockBlockStates.StructureVoid, solid: false, hardness: 0f);
        BlockRegistry.RegisterPluginBlock("minecraft:bedrock", BedrockBlockStates.Bedrock, hardness: -1f);
        BlockRegistry.RegisterPluginBlock("minecraft:dirt", BedrockBlockStates.Dirt, hardness: 0.5f);
        BlockRegistry.RegisterPluginBlock("minecraft:grass_block", BedrockBlockStates.GrassBlock, hardness: 0.6f);
        BlockRegistry.RegisterPluginBlock("minecraft:barrier", BedrockBlockStates.Barrier, solid: false, hardness: -1f);
    }

    public static void RegisterCreativeAndAllowlist(string pluginId = "orion:minimal-items")
    {
        CuratedItemCatalog.RegisterCreativeTabEntries(
            pluginId,
            (2, "minecraft:grass_block"),
            (2, "minecraft:dirt"),
            (2, "minecraft:bedrock"),
            (1, "minecraft:cobblestone"),
            (3, "minecraft:wooden_sword"),
            (4, "minecraft:stick"));
        CuratedItemCatalog.RegisterAllowlistedIdentifiers(
            pluginId,
            "minecraft:barrier",
            "minecraft:structure_void");
    }

    public static void RegisterAll(string pluginId = "orion:minimal-items")
    {
        Reset();
        RegisterBlocks();
        RegisterCreativeAndAllowlist(pluginId);
    }
}
