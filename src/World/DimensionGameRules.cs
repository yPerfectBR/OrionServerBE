namespace Orion.World;

/// <summary>
/// Game rules scoped to a dimension.
/// </summary>
public sealed class DimensionGameRules
{
    /// <summary>
    /// Whether players can take drowning damage in this dimension.
    /// </summary>
    public bool DrowningDamage { get; set; } = true;
}
