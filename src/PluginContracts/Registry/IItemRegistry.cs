namespace Orion.PluginContracts.Registry;

public interface IItemRegistry
{
    void Register(ItemRegistration registration);

    bool IsRegistered(string identifier);
}
