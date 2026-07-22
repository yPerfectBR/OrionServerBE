# Fase 27 — Plugins de mecânica de Player

**Status:** `implemented`  
**Language twin:** [`../../en_us/plugins/27-player-mechanics-plugins.md`](../../en_us/plugins/27-player-mechanics-plugins.md)  
**Depende de:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md), [24](24-entity-mechanics-plugins.md)  
**Pré-req código:** `PlayerTraitBase` / `ISessionTickableTrait` / `IPlayerChunkView` em `Orion.Api`; session tick path estável

## 1. Goal

Extrair traits de [`src/Orion/Player/Traits/`](../../../src/Orion/Player/Traits/) para plugins. Shells `Player`, `PlayerSession`, transfer/abilities/NBT merge permanecem no core (sessão Bedrock).

## 2. Non-goals

- Mover `Player.cs` / `PlayerSession` / world transfer para plugin.
- Inventário / hunger / building / mining (já plugins).
- Tornar chunk streaming opcional sem alternativa — `orion:player-chunk-rendering` é **obrigatório** para um servidor jogável com plugins enabled.

## 3. Plugins criados

| id | PackageId | Repo | provides | depend | softdepend | Origem |
|----|-----------|------|----------|--------|------------|--------|
| `orion:player-chunk-rendering` | `Orion.Plugins.PlayerChunkRendering` | `orion-player-chunk-rendering` | `orion:player-chunk-rendering` | — | — | `PlayerChunkRenderingTrait.cs` |
| `orion:player-debug` | `Orion.Plugins.PlayerDebug` | `orion-player-debug` | `orion:player-debug` | — | `orion:player-chunk-rendering` | `DebugTrait.cs` |

Facade Api (Orion.Api **0.1.9**): `ISessionTickableTrait`, `IPlayerChunkView`, `IPlayerDebugHud`, bridges de chunk em `IDimension`, lifecycle em `EntityTraitBase`, `ChunkViewMath`. Call sites do host usam `GetTrait<IPlayerChunkView>()` / `IPlayerDebugHud`. Plugins referenciam **Orion.Protocol** para LevelChunk / Publisher / RemoveActor / tip (sem `Orion.dll`).

### Boot “survival mínimo”

A fase [30](30-first-run-and-boot-order.md) lista `orion:player-chunk-rendering` no conjunto recomendado (hard para experiência jogável). Sem o plugin, o join funciona mas **não há stream de chunks**.

## 4. Removido do core

- `PlayerChunkRenderingTrait.cs`, `DebugTrait.cs`
- Auto-add de `DebugTrait` em `SetLocalPlayerAsInitialized`
- Player recebe traits via registro `PlayerTraits` do plugin (`Types = minecraft:player`)

## 5. Commits (exemplo)

1. `feat(api): add session tick chunk view and dimension streaming bridges`
2. `feat(host): wire IPlayerChunkView and EntityTraitBase lifecycle dispatch`
3. `chore(sdk): bump Orion.Api to 0.1.9`
4. `refactor(orion): remove player chunk and debug traits from core`
5. `docs(plugins): mark phase 27 implemented`

Repos de plugins:

1. `feat(plugins): add orion:player-chunk-rendering`
2. `feat(plugins): add orion:player-debug`

Sem `Co-authored-by`.

## 6. Acceptance tests

- [x] Join + stream de chunks funciona só com o plugin de chunk rendering carregado.
- [x] Sem o plugin, comportamento documentado (sem stream; first-run marca o plugin como obrigatório).
- [x] Debug trait opcional (`orion:player-debug` + `/debughud`).
- [x] Template NuGet/CI conforme [22](22-vanilla-extraction-overview.md) §8 (`0.1.9` Api + Protocol).

## 7. Status

`implemented` — `orion:player-chunk-rendering` / `orion:player-debug`; facade Api **0.1.9** (`IPlayerChunkView`, bridges de chunk, session tick); traits removidos do core.
