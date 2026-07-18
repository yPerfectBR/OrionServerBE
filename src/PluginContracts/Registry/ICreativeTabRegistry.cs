namespace Orion.PluginContracts.Registry;

public interface ICreativeTabRegistry
{
    /// <summary>Category: 1 Construction, 3 Equipment, 4 Items. Category 2 Nature is reserved for core.</summary>
    void AddEntry(string pluginId, int category, string identifier);
}
