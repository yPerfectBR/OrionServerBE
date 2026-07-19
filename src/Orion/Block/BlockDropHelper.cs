namespace Orion.Block;

using Orion.Item;

public static class BlockDropHelper
{
    public static List<ItemStack> GenerateLootFromBlock(Block block)
    {
        string id = block.Identifier;
        if (id.Equals("minecraft:air", StringComparison.Ordinal) || id.Equals("minecraft:barrier", StringComparison.Ordinal))
        {
            return [];
        }

        ItemType? type = ItemType.Get(id);
        if (type is null)
        {
            return [];
        }

        return [new ItemStack(type, 1)];
    }
}
