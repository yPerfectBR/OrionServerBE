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

        // Protocol air fallback when no block/item content plugins are loaded.
        return string.Equals(type.Identifier, "minecraft:air", StringComparison.Ordinal)
            ? BedrockBlockStates.Air
            : 0;
    }
}
