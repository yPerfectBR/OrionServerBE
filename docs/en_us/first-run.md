# First run (OrionServer)

Orion starts with an **empty** creative menu and **no** native content blocks. Nature, Construction, Equipment, and Items stay empty until a plugin registers them (typically `orion:minimal-items`).

## World generator (default: void)

New worlds use generator **`void`** (empty chunks). Default spawn is `[0, -57, 0]`. The core ships only the **`void`** builtin — there is no silent fallback to void for other ids.

To get flat terrain (bedrock / dirt / grass at Y −64…−60):

1. Enable plugins and deploy `orion:minimal-items` + `orion:superflat` (superflat depends on minimal-items for block ids).
2. Set the overworld dimension generator:

```json
"generator": "superflat"
```

Without the plugin, `generator: "superflat"` (or any unknown / empty generator) **refuses to boot** with a clear error. Dimension `identifier` must be a known Bedrock dimension (`overworld` / `nether` / `the_end`); empty or unknown values also fail boot. LevelDB chunks with unknown blocks fail hard instead of rewriting to air.

## Empty creative menu

Bedrock often shows the **entire** creative inventory as empty when Construction / Equipment / Items have no items. That is a client UI constraint.

On boot, if those tabs are still empty, Orion logs a warning pointing here.

## Recommended fix (`orion:minimal-items`)

1. Build the plugin (emits `plugins/orion:minimal-items/orion.minimal-items.dll` next to `plugin.json`):

```bash
dotnet build plugins/orion:minimal-items/OrionMinimalItems.csproj
```

2. Opt in via `config/server.json` (default is **disabled**):

```json
"Plugins": {
  "Enabled": true,
  "Directory": "plugins"
}
```

3. Restart. The host loads the plugin **only via McMaster** (folder with `plugin.json`). The plugin registers:

- Six Bedrock blocks (`air`, `structure_void`, `bedrock`, `dirt`, `grass_block`, `barrier`)
- Nature → grass, dirt, bedrock  
- Construction → cobblestone  
- Equipment → wooden sword  
- Items → stick  

**Note:** plugin-capable host is **managed** (.NET), not Native AOT.

## Health / hunger (attributes plugin)

Core does **not** include gameplay health or hunger. For vanilla behavior:

```bash
dotnet build plugins/orion:attributes/OrionAttributes.csproj
```

With `Plugins.Enabled: true`, the plugin registers **Api-only** traits (`EntityHealthTrait`, `PlayerHungerTrait`) plus services (`provides: orion:attributes`, `orion:health`, `orion:hunger`). On join it re-enables Health/Hunger HUD and syncs Bedrock attributes (`minecraft:health`, `minecraft:player.hunger`, …). Other plugins consume via `IAttributesApi` / `IEntityHealthService` / `IPlayerHungerService` / `IPlayerItemUseHandler`. Without it, vitals stay hidden and host health/hunger/food bridges are no-ops. Prefer loading alongside `orion:inventory` (softdepend) so food use can decrement stacks. Does **not** require `orion:entity-attributes`.

## Entity mechanics (phase 24)

Core used to ship gravity / collision / movement / air-supply / equipment on `minecraft:item` and players. Those traits move to first-party plugins:

```bash
dotnet build plugins/orion:entity-gravity/OrionEntityGravity.csproj
dotnet build plugins/orion:entity-collision/OrionEntityCollision.csproj
dotnet build plugins/orion:entity-movement/OrionEntityMovement.csproj
dotnet build plugins/orion:entity-air-supply/OrionEntityAirSupply.csproj
# plus entity-equipment, item-entity, entity-attributes as extracted
```

Without the entity-movement (+ collision/gravity) set, item drops do not simulate physics. Without `orion:entity-air-supply` + `orion:attributes`, drowning damage is a no-op.

## Block orientation (phase 25)

Core does **not** include direction / cardinal / facing block traits. When placed blocks must pick orientation from look direction:

```bash
dotnet build plugins/orion:block-direction/OrionBlockDirection.csproj
dotnet build plugins/orion:block-cardinal/OrionBlockCardinal.csproj
dotnet build plugins/orion:block-facing/OrionBlockFacing.csproj
```

`block-cardinal` and `block-facing` softdepend `block-direction` (compile-time reference). Without them, blocks with those states keep the default permutation.

## Item mechanics (phase 26)

Core does **not** include item durability or item-debug traits. When tools need durability binding / debug hooks:

```bash
dotnet build plugins/orion:item-durability/OrionItemDurability.csproj
dotnet build plugins/orion:item-debug/OrionItemDebug.csproj
```

`item-durability` binds via `minecraft:durability` (`ProcessDamage` stub). `item-debug` does not auto-bind to item types (opt-in).

## Player chunk / debug (phase 27)

Core does **not** stream chunks or attach the debug tip HUD. **`orion:player-chunk-rendering` is required** for a playable world (without it, join works but no LevelChunk stream). `orion:player-debug` is optional (`/debughud`).

```bash
dotnet build plugins/orion:player-chunk-rendering/OrionPlayerChunkRendering.csproj
dotnet build plugins/orion:player-debug/OrionPlayerDebug.csproj
```

Traits bind to `minecraft:player` via `PlayerTraits`. Host call sites use `IPlayerChunkView` / `IPlayerDebugHud` (Orion.Api **0.1.9**).

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

In `IOrionPlugin.Load(IPluginLoadContext)`, call `context.Registries.CreativeTabs.AddEntry(pluginId, category, identifier)` **before** catalog init (server boot already orders this). Categories: 1 Construction, 2 Nature, 3 Equipment, 4 Items. Identifiers must exist in the vanilla item palette.

See: [creative-inventory.md](creative-inventory.md) · [plugins/README.md](plugins/README.md) · [plugins/20-plugin-developer-guide.md](plugins/20-plugin-developer-guide.md).

## External / deep plugins (SDK)

Third-party plugins that need world mutation, inventory, custom blocks/items, or entity APIs should target the published SDK (`Orion.PluginContracts` + `Orion.Api` + `Orion.Gameplay.Api`) — see [plugins/09-sdk-overview.md](plugins/09-sdk-overview.md). Authors do not need to clone this monorepo once NuGet packages are published. Implementation status of that train is tracked in [plugins/18-sdk-ai-implementation-checklist.md](plugins/18-sdk-ai-implementation-checklist.md).
