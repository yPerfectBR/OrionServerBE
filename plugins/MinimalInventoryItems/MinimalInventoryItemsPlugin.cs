using Orion.PluginContracts;

namespace MinimalInventoryItems;

/// <summary>
/// Sample opt-in plugin loaded exclusively via McMaster.
/// </summary>
public sealed class MinimalInventoryItemsPlugin : IOrionPlugin
{
    public string Id => "MinimalInventoryItems";

    public Version Version { get; } = new(1, 0, 0);

    public void Load(IPluginLoadContext context)
    {
        context.Registries.CreativeTabs.AddEntry(Id, 1, "minecraft:cobblestone");
        context.Registries.CreativeTabs.AddEntry(Id, 3, "minecraft:wooden_sword");
        context.Registries.CreativeTabs.AddEntry(Id, 4, "minecraft:stick");
    }

    public void OnEnable(IPluginContext context) => _ = context;

    public void OnWorldInitialize(IWorldInitContext context) => _ = context;

    public void OnDisable(IPluginContext context) => _ = context;
}
