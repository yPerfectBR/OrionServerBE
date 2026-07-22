using Orion.Config;
using Orion.World.Block;

namespace Orion.World.Tests;

public sealed class WorldBootstrapValidationTests
{
    [Fact]
    public void ValidateDimension_Overworld_Succeeds()
    {
        WorldBootstrapValidation.ValidateDimension(new DimensionConfig
        {
            Identifier = "overworld",
            Type = 0,
            Generator = "void"
        });
    }

    [Fact]
    public void ValidateDimension_EmptyIdentifier_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            WorldBootstrapValidation.ValidateDimension(new DimensionConfig
            {
                Identifier = "  ",
                Type = 0
            }));
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDimension_UnknownIdentifier_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            WorldBootstrapValidation.ValidateDimension(new DimensionConfig
            {
                Identifier = "aether",
                Type = 0
            }));
        Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateDimension_UnknownType_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            WorldBootstrapValidation.ValidateDimension(new DimensionConfig
            {
                Identifier = "overworld",
                Type = 99
            }));
        Assert.Contains("does not exist", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class WorldBlockPermutationTests
{
    [Fact]
    public void Resolve_KnownIdentifier_Succeeds()
    {
        Assert.Equal("minecraft:bedrock", BlockPermutation.Resolve("minecraft:bedrock").Identifier);
    }

    [Fact]
    public void Resolve_UnknownOrEmpty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => BlockPermutation.Resolve("minecraft:stone"));
        Assert.Throws<InvalidOperationException>(() => BlockPermutation.Resolve(""));
        Assert.Throws<InvalidOperationException>(() => BlockPermutation.Resolve(999_999));
    }
}
