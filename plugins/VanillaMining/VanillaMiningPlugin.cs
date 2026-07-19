using Orion.Gameplay;
using Orion.PluginContracts;

namespace VanillaMining;

public sealed class VanillaMiningPlugin : IOrionPlugin
{
    public string Id => "VanillaMining";

    public Version Version { get; } = new(1, 0, 0);

    public void Load(IPluginLoadContext context) => _ = context;

    public void OnEnable(IPluginContext context)
    {
        MiningGameplayServices services = new();
        context.Services.Register<IVanillaMiningApi>(services, this);
        context.Services.Register<IPlayerBlockBreakHandler>(services, this);
    }

    public void OnWorldInitialize(IWorldInitContext context) => _ = context;

    public void OnDisable(IPluginContext context) => _ = context;
}
