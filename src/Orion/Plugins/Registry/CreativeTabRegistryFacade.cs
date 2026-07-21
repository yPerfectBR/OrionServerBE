using Orion.Config;
using Orion.PluginContracts.Registry;
using Orion.Protocol.Registry;
using Log = Orion.Logger.Logger;

namespace Orion.Plugins.Registry;

internal sealed class CreativeTabRegistryFacade(ContentRegistriesCore core) : ICreativeTabRegistry
{
    public void AddEntry(string pluginId, int category, string identifier) =>
        AddEntryCore(pluginId, category, identifier);

    internal void AddEntryCore(string pluginId, int category, string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        core.ThrowIfCreativeFrozen();

        if (category is < 1 or > 4)
        {
            Log.Warn(
                LogCategory.Plugins,
                "CreativeTabs: invalid category {0} from plugin '{1}' for '{2}'.",
                category,
                pluginId,
                identifier);
            return;
        }

        if (!core.TryClaimIdentifier(pluginId, identifier))
        {
            return;
        }

        CuratedItemCatalog.RegisterCreativeTabEntries(pluginId, (category, identifier));
    }
}
