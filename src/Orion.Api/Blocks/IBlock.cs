using Orion.Api.Math;

namespace Orion.Api.Blocks;

public interface IBlockType
{
    string Identifier { get; }
    float Hardness { get; }
    bool Solid { get; }
    bool Air { get; }
    IReadOnlyList<string> Tags { get; }
}

public interface IBlockPermutation
{
    IBlockType Type { get; }
    int NetworkId { get; }
}

public interface IBlock
{
    IBlockType Type { get; }
    IBlockPermutation Permutation { get; }

    /// <summary>Triggers host break side-effects (drops / block traits). Call after a successful break.</summary>
    void NotifyBroken(IPlayer breaker, BlockPos blockPosition);
}

/// <summary>Host-registered factory for creating blocks without referencing Orion.dll.</summary>
public interface IBlockFactory
{
    IBlock Create(string identifier);
    IBlock? TryCreate(string identifier);
    IBlockPermutation? TryGetDefaultPermutation(string identifier);
}

/// <summary>
/// Stable entry point for plugins. Host wires <see cref="IBlockFactory"/> at boot
/// via <see cref="SetFactory"/>.
/// </summary>
public static class Blocks
{
    private static IBlockFactory? _factory;

    public static void SetFactory(IBlockFactory factory) =>
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public static IBlock Create(string identifier)
    {
        IBlockFactory factory = _factory
            ?? throw new InvalidOperationException("Blocks factory is not registered. Host must call Blocks.SetFactory at boot.");
        return factory.Create(identifier);
    }

    public static IBlock? TryCreate(string identifier) => _factory?.TryCreate(identifier);

    public static IBlockPermutation? TryGetDefaultPermutation(string identifier) =>
        _factory?.TryGetDefaultPermutation(identifier);
}
