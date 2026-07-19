namespace Orion.World;

/// <summary>
/// Game rules scoped to a dimension.
/// </summary>
public sealed class DimensionGameRules
{
    public bool ShowCoordinates { get; set; } = true;

    public bool ShowDaysPlayed { get; set; }

    public bool DoDayLightCycle { get; set; } = true;

    public bool DoImmediateRespawn { get; set; }

    public bool DoTileDrops { get; set; } = true;

    public bool KeepInventory { get; set; }

    public bool FallDamage { get; set; } = true;

    public bool FireDamage { get; set; } = true;

    /// <summary>
    /// Whether players can take drowning damage in this dimension.
    /// </summary>
    public bool DrowningDamage { get; set; } = true;

    public int RandomTickSpeed { get; set; } = 1;

    public bool LocatorBar { get; set; }
}
