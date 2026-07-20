# Phase 17 — Vanilla\* dogfooding (final)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/17-sdk-vanilla-dogfood.md`](../../pt_br/plugins/17-sdk-vanilla-dogfood.md)  
**Depends on:** [10](10-sdk-packages-versioning.md)–[15](15-sdk-protocol-escape.md)

## 1. Goal

Migrate all first-party Vanilla\* plugins to the **same final SDK packages** used by third parties: no `ProjectReference` to `Orion.csproj`, no production `InternalsVisibleTo`, traits/services/events typed against Orion.Api / Gameplay.Api. This proves the SDK is complete.

## 2. Non-goals

- Rewriting vanilla gameplay behavior.
- Splitting Vanilla\* into more plugins than already exist.

## 3. Migration order (final — one train, sequenced commits)

Order respects hard depends already in manifests:

1. **orion:containers** — implements `IContainer`; provides `orion:containers`  
2. **orion:inventory** — `depend: orion:containers`  
3. **orion:attributes** — softdepend inventory  
4. **orion:building** — softdepend inventory  
5. **orion:mining** — softdepend inventory  
6. **orion:block-containers** — `depend: orion:containers, orion:inventory`  
7. **orion:creative-fillers** — contracts only (verify)

Each step: switch csproj → fix usings → build → load smoke test.

## 4. Per-plugin checklist

### All Vanilla\*

| Task | Done when |
|------|-----------|
| Remove `ProjectReference` Orion | csproj has PackageReference / ProjectReference only to Api packs + PluginContracts (+ sibling plugin Api if any) |
| Remove from Orion `InternalsVisibleTo` | Orion.csproj no longer lists this plugin |
| Replace `Player` params at boundaries | `IPlayer` |
| Replace `ItemStack` at boundaries | `IItemStack` where interface-typed |
| Signals | Subscribe/emit `Orion.Api.Events.*` |
| Traits | Register via `context.Registries.*Traits` |
| Protocol | Allowed as private implementation detail ([15](15-sdk-protocol-escape.md)) |

### VanillaContainers

- `Container` / `EntityContainer` implement `Orion.Api.Containers.IContainer`.
- Keep `InternalsVisibleTo` **only** if still required for `occupants` — **final target: make necessary members public on Api surface or friend via public API**, then drop IVT to Inventory/Blocks too.
- ProjectReference to sibling plugins removed; Inventory/Blocks PackageReference or ProjectReference **VanillaContainers** only as plugin dependency at runtime (compile: reference VanillaContainers project **or** a future `Orion.Containers.Runtime` — **final choice: Inventory/Blocks ProjectReference VanillaContainers.csproj with Private=false** is acceptable **inside the monorepo**, but **must not** reference Orion.csproj. External authors use Gameplay.Api only.)

**Clarification for monorepo:** First-party plugins may `ProjectReference` **other plugin projects** and **SDK projects** (`Orion.Api`, `Orion.Gameplay.Api`, `PluginContracts`). They must **not** `ProjectReference` `Orion.csproj`. CI can use ProjectReference to SDK projects instead of NuGet for same-repo builds; NuGet is mandatory for external authors.

### VanillaInventory

- Register services from Gameplay.Api.
- Own PacketIds unchanged.
- Handlers use `IPlayer` / `IContainer`.

### VanillaBuilding / VanillaMining

- Implement handlers with `IPlayer`, `BlockPos` from Orion.Api.Math.
- No Orion concrete imports except Protocol if still needed for transactions — prefer core calling into handlers only.

### VanillaAttributes

- Health/hunger services on `IEntity` / `IPlayer`.
- Remove need to share `typeof(Server)` in McMaster.

### VanillaContainerBlocks

- Traits subclass `BlockTraitBase` from Orion.Api.
- Chest/barrel open emits `PlayerOpenContainerSignal` (Api events).

## 5. Tests project

[`tests/Orion.Game.Tests`](../../../tests/Orion.Game.Tests/Orion.Game.Tests.csproj):

- May ProjectReference Orion + Vanilla\* + SDK projects.
- Inventory tests construct containers via VanillaContainers / Api types, not removed Orion.Container.

## 6. File touch list

| Path | Change |
|------|--------|
| `plugins/Vanilla*/**/*.csproj` | Drop Orion ProjectReference |
| `src/Orion/Orion.csproj` | Remove Vanilla InternalsVisibleTo entries |
| `src/Orion/Plugins/PluginHost.cs` | Stop sharing `Server` assembly for plugins |
| All Vanilla sources | Usings / types |
| Docs first-run | Already describe plugin graph; ensure SDK note |

## 7. Acceptance tests

- `rg ProjectReference.*Orion.csproj plugins/` → no matches.
- `rg InternalsVisibleTo.*Vanilla src/Orion/Orion.csproj` → no matches.
- Full server boot with all Vanilla\* enabled: inventory, place, mine, chest, attributes HUD.
- External DeepOre sample from [16](16-sdk-external-plugin-guide.md) loads beside Vanilla\*.
- `dotnet test tests/Orion.Game.Tests` green.

## 8. Migration notes

- Do not leave a hybrid “Vanilla still references Orion for one release” — the dogfood PR train completes the final state.
- Sibling ProjectReferences between Vanilla plugins are fine; Orion implementation reference is not.

## 9. Status

`spec`
