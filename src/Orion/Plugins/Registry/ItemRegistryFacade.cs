using Orion.PluginContracts.Registry;
using Orion.Protocol.Registry;

namespace Orion.Plugins.Registry;

internal sealed class ItemRegistryFacade(ContentRegistriesCore core) : IItemRegistry
{
    public void Register(ItemRegistration registration) =>
        throw new InvalidOperationException("Use plugin-scoped registries from IPluginLoadContext.Registries.");

    internal void Register(string pluginId, ItemRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.Identifier);
        core.ThrowIfItemsFrozen();

        if (!core.TryClaimIdentifier(pluginId, registration.Identifier))
        {
            return;
        }

        CuratedItemCatalog.RegisterAllowlistedIdentifiers(pluginId, registration.Identifier);
        core.MarkItemRegistered(registration.Identifier);

        if (registration.Creative && registration.CreativeCategory is int category)
        {
            // Ownership already claimed above; add tab entry without re-claiming.
            core.ThrowIfCreativeFrozen();
            if (category is >= 1 and <= 4 && category != 2)
            {
                CuratedItemCatalog.RegisterCreativeTabEntries(pluginId, (category, registration.Identifier));
            }
        }
    }

    public bool IsRegistered(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return core.IsItemRegistered(identifier);
    }
}
