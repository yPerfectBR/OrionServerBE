using Orion.Entity.Traits;
using Orion.Gameplay;
using Orion.Item.Traits;
using Orion.PluginContracts;
using Orion.PluginContracts.Services;
using System.Reflection;

namespace VanillaAttributes;

/// <summary>
/// Opt-in vanilla vitals: health, hunger, food use. Exposes
/// <see cref="IVanillaAttributesApi"/> / <see cref="IEntityHealthService"/> /
/// <see cref="IPlayerHungerService"/> for other plugins.
/// </summary>
public sealed class VanillaAttributesPlugin : IOrionPlugin
{
    public string Id => "VanillaAttributes";

    public Version Version { get; } = new(1, 0, 0);

    public void Load(IPluginLoadContext context)
    {
        _ = context;
        Assembly assembly = typeof(VanillaAttributesPlugin).Assembly;
        EntityTraitRegistry.RegisterFromAssembly(assembly);
        ItemTraitRegistry.RegisterFromAssembly(assembly);
    }

    public void OnEnable(IPluginContext context)
    {
        AttributeGameplayServices services = new();
        context.Services.Register<IVanillaAttributesApi>(services, this);
        context.Services.Register<IEntityHealthService>(services, this);
        context.Services.Register<IPlayerHungerService>(services, this);
        context.Services.Register<IPlayerItemUseHandler>(services, this);
    }

    public void OnWorldInitialize(IWorldInitContext context) => _ = context;

    public void OnDisable(IPluginContext context) => _ = context;
}
