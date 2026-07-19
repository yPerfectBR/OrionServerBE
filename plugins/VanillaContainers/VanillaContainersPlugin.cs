using Orion.Block.Traits;
using Orion.PluginContracts;

namespace VanillaContainers;

public sealed class VanillaContainersPlugin : IOrionPlugin
{
    public string Id => "VanillaContainers";

    public Version Version { get; } = new(1, 0, 0);

    public void Load(IPluginLoadContext context)
    {
        _ = context;
        BlockTraitRegistry.RegisterFromAssembly(typeof(VanillaContainersPlugin).Assembly);
    }

    public void OnEnable(IPluginContext context) => _ = context;

    public void OnWorldInitialize(IWorldInitContext context) => _ = context;

    public void OnDisable(IPluginContext context) => _ = context;
}
