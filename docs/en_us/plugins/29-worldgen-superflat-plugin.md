# Phase 29 — Worldgen: Superflat plugin and void in core

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/29-worldgen-superflat-plugin.md`](../../pt_br/plugins/29-worldgen-superflat-plugin.md)  
**Depends on:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md)  
**Follow-up:** [28](28-minimal-content-and-empty-core.md) (`orion:minimal-blocks` — deferred)

## 1. Goal

1. Remove `SuperFlatGenerator` from [`GeneratorFactory`](../../../src/World/Generation/GeneratorFactory.cs) builtins.
2. Ship `orion:superflat` registering generator id `superflat` via `IGeneratorRegistry` in **`Load`** (before freeze).
3. Core keeps only the **`void`** builtin (unknown → void fallback).
4. Generation stays **synchronous** via `Orion.Api.Worldgen.WorldGeneratorBase`.

## 2. Non-goals

- Advanced terrain / biomes.
- Mandatory parallel pregen.
- Creating `orion:minimal-blocks` in this phase (block ids resolve via host static table).

## 3. Plugin

| Field | Value |
|-------|--------|
| id | `orion:superflat` |
| PackageId | `Orion.Plugins.Superflat` |
| Repo | `OrionBedrock/orion-superflat` |
| provides | `orion:superflat` |
| depend | `[]` (minimal-blocks deferred) |
| API | `WorldGeneratorBase` + `IChunkGenerationContext` |

Layers (baseY −64): bedrock → 3× dirt → grass_block.

Register: `context.Registries.Generators.Register("superflat", typeof(SuperFlatWorldGenerator))` in `Load`.

## 4. Core changes

```csharp
// GeneratorFactory builtins
"void" => new VoidGenerator(),
_ => new VoidGenerator()
```

- Plugin types subclass `WorldGeneratorBase`; host wraps with `ApiGeneratorAdapter`.
- `SuperFlatGenerator.cs` removed from World.

## 5. Acceptance tests

- [x] Without plugin: `Create("superflat")` → `VoidGenerator`.
- [x] With Api registration: layers match previous behavior.
- [ ] `depend` minimal-blocks — deferred (phase 28).
- [x] Plugin CI: PackageReferenceTests + smoke boot.

## 6. Status

`implemented` (2026-07-21). Minimal-blocks dependency deferred to phase 28.
