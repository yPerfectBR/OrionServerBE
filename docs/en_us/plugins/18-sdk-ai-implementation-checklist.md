# Phase 18 — AI implementation checklist (SDK)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/18-sdk-ai-implementation-checklist.md`](../../pt_br/plugins/18-sdk-ai-implementation-checklist.md)  
**Depends on:** [09](09-sdk-overview.md)–[17](17-sdk-vanilla-dogfood.md)

## 1. Goal

Give implementers (human or AI) a **strict PR/commit order** to build the final SDK described in phases 09–17 **without inventing temporary architectures**. Each step leaves the tree buildable and moves toward the Definition of Done below.

Companion to platform checklist [08](08-ai-implementation-checklist.md) (phases 1–7). This document covers **only** the SDK train.

## 2. Non-goals

- Re-implementing McMaster/lifecycle/registries shells already marked `implemented` in 01–07.
- Shipping partial “compile against Orion.dll” authoring paths.

## 3. PR / commit sequence

Execute **in order**. Do not skip ahead to Vanilla dogfood before Api surfaces exist.

| Step | Spec doc | Work | Exit criteria |
|------|----------|------|---------------|
| S0 | [19](19-manifest-v2.md), [20](20-plugin-developer-guide.md), [21](21-plugin-repo-layout.md) | Manifest v2 parser, `LogCategory.Plugins`, `orion:*` layout | Boot validates v2; plugins load from `orion.*.dll` |
| S1 | [10](10-sdk-packages-versioning.md) | Create `src/Orion.Api` + `src/Orion.Gameplay.Api` projects (empty/skeleton); pack metadata; wire Orion.csproj ProjectReferences; expand SharedAssemblies | `dotnet pack` three packages |
| S2 | [11](11-sdk-orion-api-surface.md) | Add facade interfaces; implement on Player/Entity/Block/Item/Dimension adapters; replace PluginContracts stubs; `IContainer` in Orion.Api.Containers | Game.Tests compile; `IPlayer` usable from a scratch plugin project referencing Api |
| S3 | [12](12-sdk-registries-traits.md) | Rich Block/ItemRegistration; trait registries on IContentRegistries; BlockTraitBase/ItemTraitBase in Api | Custom block+trait sample registers in Load |
| S4 | [13](13-sdk-events-signals.md) | Move signals to Orion.Api.Events; add new signals + emit sites | Cancel place/eat tests |
| S5 | [14](14-sdk-gameplay-services.md) | Move Gameplay interfaces to Gameplay.Api; retarget core+Vanilla to IPlayer | Services TryGet from external plugin project |
| S6 | [15](15-sdk-protocol-escape.md) | IOutboundPacket adapters; BlockNetwork helpers; Protocol packable | Plugin without Protocol can update blocks via helpers |
| S7 | [17](17-sdk-vanilla-dogfood.md) | Migrate Vanilla\* in listed order; strip IVT | No plugin → Orion.csproj; full smoke |
| S8 | [16](16-sdk-external-plugin-guide.md) | Template + example plugin using **packed NuGet** from artifacts | Fresh restore works |
| S9 | Docs | Flip 09–18 (and hub) status to `implemented`; update first-run | Docs match code |
| S10 | [20](20-plugin-developer-guide.md), [21](21-plugin-repo-layout.md) | Keep dev guide + layout aligned with shipped manifest/plugins | External author can follow 20 without reading source |

## 4. Global Definition of Done

- [ ] `Orion.PluginContracts`, `Orion.Api`, `Orion.Gameplay.Api` pack with the same Version train  
- [ ] McMaster shares those three assemblies (plus allowlisted Vanilla\*.Api if any)  
- [ ] No `ProjectReference` to `Orion.csproj` under `plugins/`  
- [ ] No `InternalsVisibleTo` Vanilla\* on Orion  
- [ ] MinimalInventoryItems still contracts-only  
- [ ] At least one deep sample builds from NuGet only  
- [ ] Vanilla\* load: inventory, building, mining, containers, attributes  
- [ ] `dotnet test` (Game.Tests + relevant) green  
- [ ] Docs 09–18 status `implemented`  

## 5. Anti-patterns (reject in review)

- Adding `InternalsVisibleTo` for a new plugin  
- Sharing `Orion.dll` or `Protocol` in SharedAssemblies “to make Vanilla compile”  
- Public Gameplay.Api methods taking concrete `Player` / `DataPacket`  
- Documenting a temporary DevKit HintPath  
- Leaving `IOrionServer` / `IOrionWorld` empty stubs  

## 6. Acceptance tests (aggregate)

Mirror each phase’s §Acceptance tests; CI should cover:

- Pack + local nuget feed restore for sample plugin  
- Boot with Plugins.Enabled + all Vanilla\*  
- api mismatch fatal  
- Place cancel signal  

## 7. Status

`spec` — flip to `implemented` only when §4 is complete.
