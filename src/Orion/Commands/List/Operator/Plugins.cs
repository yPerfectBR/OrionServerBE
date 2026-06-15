namespace Orion.Commands.List.Operator;

using Orion.Commands;

public class PluginsCommand : Command
{
    public PluginsCommand() : base("plugins", "List loaded plugins")
    {
        Permissions.Add("basalt.op");
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        _ = state;
        return CommandResult.Message("§r§7Plugins (§a0§7)\n§7` No plugins loaded.", true);
    }
}
