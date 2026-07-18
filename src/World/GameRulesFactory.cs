using Orion.Config;
using Orion.Protocol.Types;

namespace Orion.World;

/// <summary>
/// Builds network and runtime gamerule state from world config.
/// </summary>
public static class GameRulesFactory
{
    public static List<GameRule> CreateNetworkRules(GamerulesConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return
        [
            Bool("showcoordinates", config.ShowCoordinates),
            Bool("showdaysplayed", config.ShowDaysPlayed),
            Bool("dodaylightcycle", config.DoDayLightCycle),
            Bool("doimmediaterespawn", config.DoImmediateRespawn),
            Bool("dotiledrops", config.DoTileDrops),
            Bool("keepinventory", config.KeepInventory),
            Bool("falldamage", config.FallDamage),
            Bool("firedamage", config.FireDamage),
            Bool("drowningdamage", config.DrowningDamage),
            Int("randomtickspeed", config.RandomTickSpeed),
            Bool("locatorbar", config.LocatorBar)
        ];
    }

    public static void Apply(DimensionGameRules rules, GamerulesConfig config)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(config);

        rules.ShowCoordinates = config.ShowCoordinates;
        rules.ShowDaysPlayed = config.ShowDaysPlayed;
        rules.DoDayLightCycle = config.DoDayLightCycle;
        rules.DoImmediateRespawn = config.DoImmediateRespawn;
        rules.DoTileDrops = config.DoTileDrops;
        rules.KeepInventory = config.KeepInventory;
        rules.FallDamage = config.FallDamage;
        rules.FireDamage = config.FireDamage;
        rules.DrowningDamage = config.DrowningDamage;
        rules.RandomTickSpeed = config.RandomTickSpeed;
        rules.LocatorBar = config.LocatorBar;
    }

    static GameRule Bool(string name, bool value) => new()
    {
        Name = name,
        CanBeModifiedByPlayer = false,
        Value = value
    };

    static GameRule Int(string name, int value) => new()
    {
        Name = name,
        CanBeModifiedByPlayer = false,
        Value = value < 0 ? 0 : value
    };
}
