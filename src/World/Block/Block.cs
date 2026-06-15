namespace Orion.World.Block;

/// <summary>
/// Minimal in-world block wrapper.
/// </summary>
public sealed class Block
{
    public BlockPermutation Permutation { get; private set; }

    public Block(BlockPermutation permutation) => Permutation = permutation;

    public void SetPermutation(BlockPermutation permutation) => Permutation = permutation;
}
