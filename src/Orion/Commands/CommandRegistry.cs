using Orion.Player;

namespace Orion.Commands;

public sealed class Command
{
    public string Name { get; init; } = "";

    public List<string> Aliases { get; init; } = [];

    public List<string> Permissions { get; init; } = [];
}

public sealed class CommandRegistry
{
    public const string PermissionDeniedMessage = "§cYou do not have permission to run this command.";

    public void SendAvailableCommands(Server server, Player.Player player)
    {
        _ = server;
        _ = player;
    }

    public Command Get(string name) =>
        throw new KeyNotFoundException($"Could not find command '{name}'.");

    public CommandResult Execute(Server server, Player.Player player, string line)
    {
        _ = server;
        _ = player;
        _ = line;
        return CommandResult.Message("Commands not available.", success: false);
    }

    public static bool CanPlayerExecute(Command command, Player.Player player)
    {
        if (command.Permissions.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < command.Permissions.Count; i++)
        {
            if (player.Permissions.Contains(command.Permissions[i]))
            {
                return true;
            }
        }

        return false;
    }
}
