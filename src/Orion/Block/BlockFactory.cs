using Orion.Api.Blocks;
using Orion.Block;

namespace Orion.Block;

internal sealed class BlockFactory : IBlockFactory
{
    public IBlock Create(string identifier)
    {
        BlockType type = BlockType.Get(identifier)
            ?? throw new InvalidOperationException($"Unknown block type '{identifier}'.");
        return new Block(type, type.GetPermutation());
    }

    public IBlock? TryCreate(string identifier)
    {
        BlockType? type = BlockType.Get(identifier);
        return type is null ? null : new Block(type, type.GetPermutation());
    }

    public IBlockPermutation? TryGetDefaultPermutation(string identifier)
    {
        BlockType? type = BlockType.Get(identifier);
        return type?.GetPermutation();
    }
}
