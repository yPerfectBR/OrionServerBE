using Orion.Config;
using Orion.Protocol.Enums;

namespace Orion.World;

/// <summary>
/// Fail-fast checks for dimension entries before world bootstrap.
/// </summary>
public static class WorldBootstrapValidation
{
    static readonly HashSet<string> KnownDimensionIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "overworld",
        "nether",
        "the_end",
        "end"
    };

    public static void ValidateDimension(DimensionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.Identifier))
        {
            throw new InvalidOperationException("Dimension identifier is invalid: value is empty.");
        }

        if (!KnownDimensionIdentifiers.Contains(config.Identifier))
        {
            throw new InvalidOperationException($"Dimension identifier '{config.Identifier}' does not exist.");
        }

        if (!Enum.IsDefined(typeof(DimensionType), config.Type))
        {
            throw new InvalidOperationException($"Dimension type '{config.Type}' does not exist.");
        }
    }
}
