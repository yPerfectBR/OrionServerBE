# Phase 27 — Player mechanics plugins

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/27-player-mechanics-plugins.md`](../../pt_br/plugins/27-player-mechanics-plugins.md)  
**Depends on:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md), [24](24-entity-mechanics-plugins.md)  
**Code prerequisite:** `PlayerTraitBase` / `ISessionTickableTrait` / `IPlayerChunkView` in `Orion.Api`; stable session tick path

## 1. Goal

Extract traits from [`src/Orion/Player/Traits/`](../../../src/Orion/Player/Traits/) into plugins. `Player`, `PlayerSession`, transfer/abilities/NBT merge shells stay in core (Bedrock session).

## 2. Non-goals

- Moving `Player.cs` / `PlayerSession` / world transfer into a plugin.
- Inventory / hunger / building / mining (already plugins).
- Making chunk streaming optional with no replacement — `orion:player-chunk-rendering` is **required** for a playable server with plugins enabled.

## 3. Plugins created

| id | PackageId | Repo | provides | depend | softdepend | Origin |
|----|-----------|------|----------|--------|------------|--------|
| `orion:player-chunk-rendering` | `Orion.Plugins.PlayerChunkRendering` | `orion-player-chunk-rendering` | `orion:player-chunk-rendering` | — | — | `PlayerChunkRenderingTrait.cs` |
| `orion:player-debug` | `Orion.Plugins.PlayerDebug` | `orion-player-debug` | `orion:player-debug` | — | `orion:player-chunk-rendering` | `DebugTrait.cs` |

Api facade (Orion.Api **0.1.9**): `ISessionTickableTrait`, `IPlayerChunkView`, `IPlayerDebugHud`, `IDimension` chunk bridges, `EntityTraitBase` lifecycle hooks, `ChunkViewMath`. Host call sites use `GetTrait<IPlayerChunkView>()` / `IPlayerDebugHud`. Plugins reference **Orion.Protocol** for LevelChunk / Publisher / RemoveActor / tip packets (no `Orion.dll`).

### Minimal survival boot

Phase [30](30-first-run-and-boot-order.md) lists `orion:player-chunk-rendering` in the recommended set (hard requirement for a playable experience). Without it, join succeeds but **no chunk stream** (void / missing terrain).

## 4. Removed from core

- `PlayerChunkRenderingTrait.cs`, `DebugTrait.cs`
- Auto-add of `DebugTrait` on `SetLocalPlayerAsInitialized`
- Player receives chunk/debug traits via plugin `PlayerTraits` registration (`Types = minecraft:player`)

## 5. Example commits

1. `feat(api): add session tick chunk view and dimension streaming bridges`
2. `feat(host): wire IPlayerChunkView and EntityTraitBase lifecycle dispatch`
3. `chore(sdk): bump Orion.Api to 0.1.9`
4. `refactor(orion): remove player chunk and debug traits from core`
5. `docs(plugins): mark phase 27 implemented`

Plugin repos:

1. `feat(plugins): add orion:player-chunk-rendering`
2. `feat(plugins): add orion:player-debug`

No `Co-authored-by`.

## 6. Acceptance tests

- [x] Join + chunk streaming works only with the chunk-rendering plugin loaded.
- [x] Without it, behavior is documented (no stream; first-run marks plugin required).
- [x] Debug trait optional (`orion:player-debug` + `/debughud`).
- [x] NuGet/CI template per [22](22-vanilla-extraction-overview.md) §8 (`0.1.9` Api + Protocol).

## 7. Status

`implemented` — `orion:player-chunk-rendering` / `orion:player-debug`; Api facade **0.1.9** (`IPlayerChunkView`, dimension chunk bridges, session tick); core traits removed.
