namespace Orion.Item;

using Orion.Block;
using Orion.Protocol.Registry;

public static class ItemBlockRuntimeIds
{
    public static int Resolve(ItemType type)
    {
        CuratedItemCatalog.EnsureInitialized();

        if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
        {
            return type.BlockType.Permutations[0].NetworkId;
        }

        if (CuratedItemCatalog.TryGetByIdentifier(type.Identifier, out CuratedItem curated) && curated.IsBlock)
        {
            return curated.BlockStateHash;
        }

        return type.Identifier switch
        {
            "minecraft:air" => BedrockBlockStates.Air,
            "minecraft:grass_block" => BedrockBlockStates.GrassBlock,
            "minecraft:dirt" => BedrockBlockStates.Dirt,
            "minecraft:bedrock" => BedrockBlockStates.Bedrock,
            "minecraft:barrier" => BedrockBlockStates.Barrier,
            "minecraft:structure_void" => BedrockBlockStates.StructureVoid,
            _ => 0
        };
    }
}
