# Phase 2 — Lifecycle & manifest

**Status:** `implemented` (lifecycle); **dependency fields superseded by** [19 — Manifest v2](19-manifest-v2.md)  
**Language twin:** [`../../pt_br/plugins/02-lifecycle-manifest.md`](../../pt_br/plugins/02-lifecycle-manifest.md)

## 1. Goal

Define a deterministic plugin lifecycle and a **`plugin.json`** manifest so the host can order loads, fail fast on missing hard dependencies, and soft-order optional integrations — without requiring plugin A’s assembly to hard-reference plugin B.

## 2. Non-goals

- Runtime install from the internet / marketplace.
- Hot-reload of changed DLLs without process restart (may come later with unloadable ALCs).
- Host API version field in `plugin.json` (removed; see [19](19-manifest-v2.md)).

## 3. Public API sketch

> **Manifest v2:** `depend` / `softdepend` are **objects** with SemVer ranges; `loadbefore` is removed. See [19 — Manifest v2](19-manifest-v2.md).

### `plugin.json` (lifecycle fields — see [19](19-manifest-v2.md) for full schema)

```json
{
  "id": "MinimalInventoryItems",
  "version": "1.0.0",
  "description": "Fills non-Nature creative tabs",
  "authors": ["Orion"],
  "main": "MinimalInventoryItems.MinimalInventoryItemsPlugin",
  "depend": [],
  "softdepend": [],
  "loadbefore": [],
  "provides": ["orion:minimal-items", "orion:creative-tab-fillers"]
}
```

| Field | Required | Meaning |
|-------|----------|---------|
| `id` | yes | Unique plugin id; matches folder name |
| `version` | yes | SemVer plugin version |
| `main` | yes | Fully qualified type implementing `IOrionPlugin` |
| `depend` | no | Hard dependencies — see [19](19-manifest-v2.md) |
| `softdepend` | no | Optional ordering — see [19](19-manifest-v2.md) |
| `provides` | no | Capability names for discovery (`Services` / diagnostics) |

Inspired by PocketMine / Endstone `depend` / `soft_depend` / `load_before` / `provides`.

### Lifecycle

```csharp
public enum PluginState
{
    Discovered,
    Loaded,      // after Load()
    Enabled,     // after OnEnable()
    WorldReady,  // after OnWorldInitialize()
    Disabled
}
```

| Method | When | Allowed |
|--------|------|---------|
| `Load` | After ALC load, **before** `Server` exists | Pre-catalog registrations, read config files, no `Server.On` |
| `OnEnable` | After `ServerHost.Bootstrap` | Events, commands, services, messenger |
| `OnWorldInitialize` | World/dimensions ready | Block/item palette registration, generators |
| `OnDisable` | Shutdown (and future unload) | Unsubscribe, flush |

## 4. Boot / runtime sequence

1. Discover `plugins/*/plugin.json`.
2. Validate ids unique.
3. Build graph:
   - Edge `depend`: A → B means B must load before A; missing B ⇒ fatal.
   - Edge `softdepend`: same order **if B exists**; else ignore.
   - `loadbefore`: if A lists C, A before C when both exist.
4. Detect cycles ⇒ fatal with clear message.
5. For each plugin in order: McMaster load → construct `main` → `Load`.
6. Core catalog / server bootstrap.
7. `OnEnable` in same order.
8. `OnWorldInitialize` in same order (or per-world when multi-world exists).
9. On shutdown: `OnDisable` in **reverse** order.

## 5. File touch list

| Path | Change |
|------|--------|
| New `Orion.Plugins.Manifest` types in contracts or Orion | DTO + parser |
| [`src/Orion/Plugins/PluginHost.cs`](../../../src/Orion/Plugins/PluginHost.cs) | Discovery + topo sort |
| [`src/Server/Program.cs`](../../../src/Server/Program.cs) | Split Load / Enable / WorldInit calls |
| [`src/Orion/ServerHost.cs`](../../../src/Orion/ServerHost.cs) | Hook WorldInit after pregen |
| Sample `plugins/MinimalInventoryItems/plugin.json` | Add manifest |

## 6. Acceptance tests

- Missing hard `depend` fails boot with plugin id named in the error.
- Soft depend absent: dependent plugin still enables; no error.
- Soft depend present: dependency `Load`/`OnEnable` runs first.
- Cycle in `depend` fails boot.
- Duplicate `id` fails boot.
- Wrong `main` type fails that plugin (and hard-dependents), with isolated error when possible.

## 7. Migration notes from current stub

| Today | Target |
|-------|--------|
| Folder + DLL discovery | `plugin.json` required |
| Single `Load()` | Four lifecycle methods |
| No ordering | Topological sort |
| Creative registration only in Load | Still OK; WorldInit preferred for world-scoped content |

## 8. Status

`spec`

## Ordering algorithm (normative)

1. Nodes = discovered plugins.
2. For each `depend` and each **satisfied** `softdepend`, add edge `dep → plugin` (dep must come first).
3. For each `loadbefore: [X]`, add edge `plugin → X` when X exists.
4. Kahn topological sort; on leftover nodes ⇒ cycle error listing remaining ids.
5. Stable tie-break: alphabetical by `id` for determinism.

## Soft vs hard (operator guidance)

- Use **`depend`** only when the plugin **cannot start** without the other (shared native resource, mandatory API).
- Prefer **`softdepend` + Services/Messenger** (Phase 5) for optional features (economy hooks, chat formatters).
- Prefer **`provides`** so diagnostics can say “capability `orion:economy` supplied by plugin X”.
