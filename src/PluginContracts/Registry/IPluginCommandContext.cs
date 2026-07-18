namespace Orion.PluginContracts.Registry;

public interface IPluginCommandContext
{
    string SenderName { get; }

    IReadOnlyList<string> Arguments { get; }

    void Reply(string message);
}
