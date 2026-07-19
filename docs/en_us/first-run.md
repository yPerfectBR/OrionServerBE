# First run (OrionServer)

Orion starts with a **minimal** creative menu: only Nature blocks from `src/Protocol/Data/orion/items.json` (e.g. grass, dirt, bedrock). Construction, Equipment, and Items stay empty unless a plugin registers fillers.

## Empty creative menu

Bedrock often shows the **entire** creative inventory as empty when those other tabs have no items — even if Nature is correct. That is a client UI constraint, not a missing Nature packet.

On boot, if those tabs are still empty, Orion logs a warning pointing here.

## Recommended fix (sample plugin)

1. Build the sample (emits `plugins/MinimalInventoryItems/MinimalInventoryItems.dll` next to `plugin.json`):

```bash
dotnet build plugins/MinimalInventoryItems/MinimalInventoryItems.csproj
```

2. Opt in via `config/server.json` (default is **disabled**):

```json
"Plugins": {
  "Enabled": true,
  "Directory": "plugins"
}
```

3. Restart. The host loads the plugin **only via McMaster** (folder with `plugin.json`). The sample registers:

- Construction → cobblestone  
- Equipment → wooden sword  
- Items → stick  

**Note:** plugin-capable host is **managed** (.NET), not Native AOT.

## Health / hunger (attributes plugin)

Core does **not** include gameplay health or hunger. For vanilla behavior:

```bash
dotnet build plugins/VanillaAttributes/VanillaAttributes.csproj
```

With `Plugins.Enabled: true`, the plugin registers traits + services (`provides: orion:attributes`, `orion:health`, `orion:hunger`). Other plugins consume via `IVanillaAttributesApi` / `IEntityHealthService` / `IPlayerHungerService`. Without it, there is no HP/hunger/food use.

## Custom fillers

In `IOrionPlugin.Load(IPluginLoadContext)`, call `context.Registries.CreativeTabs.AddEntry(pluginId, category, identifier)` **before** catalog init (server boot already orders this). Do not put Nature (category 2) entries there — edit `orion/items.json`.

See: [creative-inventory.md](creative-inventory.md) · [plugins/README.md](plugins/README.md).
