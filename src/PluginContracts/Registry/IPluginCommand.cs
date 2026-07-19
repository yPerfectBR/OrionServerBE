namespace Orion.PluginContracts.Registry;

public interface IPluginCommand
{
    string Name { get; }

    string Description { get; }

    IReadOnlyList<string> Aliases { get; }

    void Execute(IPluginCommandContext context);
}
