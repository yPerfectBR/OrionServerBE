namespace Orion.Commands.List.Operator;

using Orion.Api;
using Orion.Commands;
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

        PlayerDebugHudMode mode = modeArgument.Value.ToLowerInvariant() switch
        {
            "off" => PlayerDebugHudMode.Off,
            "simplified" or "simple" => PlayerDebugHudMode.Simplified,
            "full" => PlayerDebugHudMode.Full,
            _ => PlayerDebugHudMode.Full
        };

        List<Player> players = ResolveTargets(state, target);
        if (players.Count == 0)
        {
            return CommandResult.Message("§cNo online player matched target.", false);
        }

        int updated = 0;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            IPlayerDebugHud? debugHud = player.GetTrait<IPlayerDebugHud>();
            if (debugHud is null)
            {
                player.SendMessage("§cDebug HUD plugin (orion:player-debug) is not loaded.");
                continue;
            }

            debugHud.SetMode(mode);
            player.SendMessage($"§7Debug HUD mode set to §a{ModeLabel(mode)}§7.");
            updated++;
        }

        if (updated == 0)
        {
            return CommandResult.Message("§cNo player had the debug HUD trait available.", false);
        }

        return CommandResult.Message($"§7Set Debug HUD to §a{ModeLabel(mode)} §7for §a{updated}§7 player(s).", true);
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

    private static string ModeLabel(PlayerDebugHudMode mode) => mode switch
    {
        PlayerDebugHudMode.Off => "off",
        PlayerDebugHudMode.Simplified => "simplified",
        PlayerDebugHudMode.Full => "full",
        _ => "full"
    };
}
