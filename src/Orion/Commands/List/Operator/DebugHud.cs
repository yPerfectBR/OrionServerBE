namespace Orion.Commands.List.Operator;

using Orion.Commands;
using Orion.Player.Traits;
using Player = global::Orion.Player.Player;

public sealed class DebugHudModeEnum : CustomEnum
{
    public static readonly string[] Values =
    [
        "off",
        "simplified",
        "simple",
        "full"
    ];

    public DebugHudModeEnum() : base("debughud_mode")
    {
    }
}

public sealed class DebugHudCommand : Command
{
    public DebugHudCommand() : base("debughud", "Set per-player debug HUD mode", ["dbg"], [])
    {
        Permissions.Add("basalt.op");

        CreateOverload()
            .Set<DebugHudModeEnum>("mode", true)
            .Set<TargetEnum>("target", false);
    }

    public override string? GetHelpMessage() =>
        "§7Usage: §a/debughud <off|simplified|full> [target]";

    public override CommandResult Execute(CommandExecutionState state)
    {
        DebugHudModeEnum? modeArgument = state.Get<DebugHudModeEnum>("mode");
        TargetEnum? target = state.Get<TargetEnum>("target");
        if (modeArgument?.Value is null)
        {
            return CommandResult.Message(GetHelpMessage() ?? "§cInvalid mode.", false);
        }

        DebugHudMode mode = modeArgument.Value.ToLowerInvariant() switch
        {
            "off" => DebugHudMode.Off,
            "simplified" or "simple" => DebugHudMode.Simplified,
            "full" => DebugHudMode.Full,
            _ => DebugHudMode.Full
        };

        List<Player> players = ResolveTargets(state, target);
        if (players.Count == 0)
        {
            return CommandResult.Message("§cNo online player matched target.", false);
        }

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            DebugTrait? debugTrait = player.GetTrait<DebugTrait>();
            if (debugTrait is null)
            {
                debugTrait = player.AddTrait(new DebugTrait(player));
                debugTrait.OnSpawn(new Orion.Entity.Traits.Types.EntitySpawnOptions(InitialSpawn: false));
            }

            debugTrait.SetMode(mode);
            player.SendMessage($"§7Debug HUD mode set to §a{ModeLabel(mode)}§7.");
        }

        return CommandResult.Message($"§7Set Debug HUD to §a{ModeLabel(mode)} §7for §a{players.Count}§7 player(s).", true);
    }

    private static List<Player> ResolveTargets(CommandExecutionState state, TargetEnum? target)
    {
        if (target is not null)
        {
            return target.Entities.OfType<Player>().ToList();
        }

        if (state.Executor is PlayerExecutor executor)
        {
            return [executor.Player];
        }

        return [];
    }

    private static string ModeLabel(DebugHudMode mode) => mode switch
    {
        DebugHudMode.Off => "off",
        DebugHudMode.Simplified => "simplified",
        DebugHudMode.Full => "full",
        _ => "full"
    };
}
