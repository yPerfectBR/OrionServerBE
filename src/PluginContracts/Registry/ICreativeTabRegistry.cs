namespace Orion.PluginContracts.Registry;

public interface ICreativeTabRegistry
{
    /// <summary>Category: 1 Construction, 2 Nature, 3 Equipment, 4 Items.</summary>
    void AddEntry(string pluginId, int category, string identifier);
}
