using Orion.PluginContracts.Registry;
using Orion.World.Generation;

namespace Orion.Plugins.Registry;

internal sealed class GeneratorRegistryFacade(ContentRegistriesCore core) : IGeneratorRegistry
{
    public void Register(string name, Type generatorType) =>
        throw new InvalidOperationException("Use plugin-scoped registries from IPluginLoadContext.Registries.");

    internal void Register(string pluginId, string name, Type generatorType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(generatorType);
        core.ThrowIfGeneratorsFrozen();

        if (!core.TryClaimGenerator(pluginId, name))
        {
            return;
        }

        GeneratorFactory.Register(name, generatorType);
    }
}
