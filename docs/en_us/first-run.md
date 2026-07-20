# First run (OrionServer)

Orion starts with a **minimal** creative menu: only Nature blocks from `src/Protocol/Data/orion/items.json` (e.g. grass, dirt, bedrock). Construction, Equipment, and Items stay empty unless a plugin registers fillers.

## Empty creative menu

Bedrock often shows the **entire** creative inventory as empty when those other tabs have no items — even if Nature is correct. That is a client UI constraint, not a missing Nature packet.

On boot, if those tabs are still empty, Orion logs a warning pointing here.

## Recommended fix (sample plugin)

1. Build the sample (emits `plugins/orion:creative-fillers/orion.creative-fillers.dll` next to `plugin.json`):

```bash
dotnet build plugins/orion:creative-fillers/OrionCreativeFillers.csproj
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
dotnet build plugins/orion:attributes/OrionAttributes.csproj
```

With `Plugins.Enabled: true`, the plugin registers traits + services (`provides: orion:attributes`, `orion:health`, `orion:hunger`). Other plugins consume via `IAttributesApi` / `IEntityHealthService` / `IPlayerHungerService`. Without it, there is no HP/hunger/food use. Prefer loading alongside `orion:inventory` (softdepend).

## Inventory, containers, building, and mining

Core does **not** include player inventory, chest/barrel, block place, or mining. Build:

```bash
dotnet build plugins/orion:containers/OrionContainers.csproj
dotnet build plugins/orion:inventory/OrionInventory.csproj
dotnet build plugins/orion:block-containers/OrionBlockContainers.csproj
dotnet build plugins/orion:building/OrionBuilding.csproj
dotnet build plugins/orion:mining/OrionMining.csproj
```

Load order is resolved from manifest v2 ([plugins/19-manifest-v2.md](plugins/19-manifest-v2.md)). Survival place/mine needs `orion:inventory`; creative place works with `orion:building` alone.

- `orion:containers` — storage/UI runtime (`provides: orion:containers`)
- `orion:inventory` — inventory/cursor/ISR (`depend: orion:containers`, `provides: orion:inventory`)
- `orion:block-containers` — chest/barrel (`depend` on containers + inventory)
- `orion:building` — place / use-on-block (`provides: orion:building`, softdepend inventory)
- `orion:mining` — crack / destroy (`provides: orion:mining`, softdepend inventory)
- API inventory: `IInventoryApi` / `IPlayerInventoryService`

## Custom fillers

In `IOrionPlugin.Load(IPluginLoadContext)`, call `context.Registries.CreativeTabs.AddEntry(pluginId, category, identifier)` **before** catalog init (server boot already orders this). Do not put Nature (category 2) entries there — edit `orion/items.json`.

See: [creative-inventory.md](creative-inventory.md) · [plugins/README.md](plugins/README.md) · [plugins/20-plugin-developer-guide.md](plugins/20-plugin-developer-guide.md).

## External / deep plugins (SDK)

Third-party plugins that need world mutation, inventory, custom blocks/items, or entity APIs should target the published SDK (`Orion.PluginContracts` + `Orion.Api` + `Orion.Gameplay.Api`) — see [plugins/09-sdk-overview.md](plugins/09-sdk-overview.md). Authors do not need to clone this monorepo once NuGet packages are published. Implementation status of that train is tracked in [plugins/18-sdk-ai-implementation-checklist.md](plugins/18-sdk-ai-implementation-checklist.md).
