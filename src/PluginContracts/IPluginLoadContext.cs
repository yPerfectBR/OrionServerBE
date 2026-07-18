using Orion.PluginContracts.Registry;

namespace Orion.PluginContracts;

public interface IPluginLoadContext
{
    IPluginManifest Manifest { get; }

    IContentRegistries Registries { get; }
}
