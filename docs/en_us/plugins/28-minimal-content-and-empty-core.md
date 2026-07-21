# Phase 28 — Minimal content and empty core

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/28-minimal-content-and-empty-core.md`](../../pt_br/plugins/28-minimal-content-and-empty-core.md)  
**Depends on:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md), [25](25-block-mechanics-plugins.md)  
**Code prerequisite:** stable `RegisterPluginBlock` / item facades; catalog freeze after `Load`

## 1. Goal

1. Remove **all** native registers from [`BlockRegistry.RegisterFromBedrockStates`](../../../src/Orion/Block/BlockRegistry.cs) (6 blocks).
2. Ship minimal content plugin(s) that re-register those blocks (and related items / runtime ids).
3. Enforce load order: **`air` (and the rest) registered before world init**; generators `depend` this content.

## 2. Non-goals

- Full vanilla palette.
- A permanent `air` stub in core (forbidden in the end state — see §4).
- Superflat (phase [29](29-worldgen-superflat-plugin.md)).

## 3. Content migrated

| Identifier | Notes |
|------------|--------|
| `minecraft:air` | Required for chunks/protocol; highest load priority |
| `minecraft:structure_void` | |
| `minecraft:bedrock` | hardness -1 |
| `minecraft:dirt` | |
| `minecraft:grass_block` | |
| `minecraft:barrier` | non-solid |

Also: Nature creative entries left `orion/items.json`; allowlist for barrier/structure_void; sample Construction / Equipment / Items fillers (ex-`creative-fillers`).

## 4. Plugin (shipped)

| id | PackageId | Repo | provides | depend | Role |
|----|-----------|------|----------|--------|------|
| `orion:minimal-items` | `Orion.Plugins.MinimalItems` | `orion-minimal-items` | `orion:minimal-items`, `orion:creative-tab-fillers` | — | Six blocks + Nature + fillers |

**Delivery choice:** one mono-plugin (renamed from `orion:creative-fillers`). Split into `minimal-blocks` + `minimal-items` later if the item footprint grows.

### `air` policy

1. `orion:minimal-items` registers `air` in `plugin.Load` (before world bootstrap).
2. `orion:superflat` declares `depend` `orion:minimal-items`.
3. Core does **not** `RegisterBlock` content. `RegisterFromBedrockStates` is a no-op.

## 5. Core changes

- Empty `RegisterFromBedrockStates`.
- Empty `orion/items.json`; Nature (category 2) accepted from plugins.
- `/give` allowlist is authoritative even when empty.
- Tests use `MinimalContentFixtures` when McMaster is not loaded.

## 6. Status

`implemented`
