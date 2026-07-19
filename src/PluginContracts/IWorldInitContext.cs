using Orion.PluginContracts.Registry;

namespace Orion.PluginContracts;

public interface IWorldInitContext
{
    IPluginManifest Manifest { get; }

    IOrionWorld World { get; }

    IContentRegistries Registries { get; }
}
