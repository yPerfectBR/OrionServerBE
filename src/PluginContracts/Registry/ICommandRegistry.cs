namespace Orion.PluginContracts.Registry;

public interface ICommandRegistry
{
    void Register(IPluginCommand command);
}
