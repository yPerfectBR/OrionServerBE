# Phase 8 — AI implementation checklist

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/08-ai-implementation-checklist.md`](../../pt_br/plugins/08-ai-implementation-checklist.md)

This document is the **implementation order** for coding agents and humans for **platform phases 01–07**. Each phase below maps to those docs. Do not skip dependency order. Do not edit architecture intent without updating the twin language docs.

**SDK / deep plugins (NuGet Orion.Api):** see [09 — SDK overview](09-sdk-overview.md) and the ordered checklist [18 — AI SDK checklist](18-sdk-ai-implementation-checklist.md). Do not mix temporary monorepo-only authoring into the SDK train.

## Global rules for implementers

1. Keep `Plugins.Enabled` default **`false`**.
2. Plugins reference **`Orion.PluginContracts`** (and, for the SDK train, **`Orion.Api` / `Orion.Gameplay.Api`**), not the Orion **implementation** assembly.
3. **McMaster is the only loader** — no `Assembly.LoadFrom` / hand-rolled ALC.
4. Type/API names in code must match these specs (same identifiers in PT/EN docs).
5. Add/extend tests under `tests/` for each phase’s acceptance list.
6. Update phase doc **Status** from `spec` → `implemented` when acceptance tests pass.
7. Managed publish only for plugin-capable builds (no Native AOT + dynamic plugins).
8. For deep gameplay / NuGet SDK work, follow [18](18-sdk-ai-implementation-checklist.md) after 01–07 are done.

---

## PR 1 — Contracts + McMaster loader (Phases 1–2 foundation)

### Implement

- [x] Create `src/PluginContracts/` (`net10.0`) with `IOrionPlugin`, contexts, manifest interfaces (stubs OK for Events/Services until later PRs).
- [x] Add `McMaster.NETCore.Plugins` to Orion host project.
- [x] Rewrite [`PluginHost`](../../../src/Orion/Plugins/PluginHost.cs) to use McMaster + `plugins/*/plugin.json`.
- [x] Topological sort for depend/softdepend/loadbefore.
- [x] Split boot: `LoadConfigured` → catalog → `ServerHost.Bootstrap` → `EnableAll` → `WorldInitialize`.
- [x] Migrate [`MinimalInventoryItems`](../../../plugins/MinimalInventoryItems/) to contracts + `plugin.json` + publish layout.
- [x] Gate/disable AOT for plugin host profile; document in README/first-run.

### Touch files

- `src/PluginContracts/**`
- `src/Orion/Plugins/**`
- `src/Orion/Orion.csproj`, `OrionServerBE.slnx`
- `src/Server/Program.cs`, `src/Orion/ServerHost.cs`
- `plugins/MinimalInventoryItems/**`
- `config/server.json` (comments/docs only if needed)

### Acceptance

- [x] Tests from [01](01-loader-contracts-mcmaster.md) §6 and [02](02-lifecycle-manifest.md) §6.

---

## PR 2 — Event bus priorities (Phase 3)

### Implement

- [x] `IEventBus` + `EventPriority` in contracts; adapter over [`Server`](../../../src/Orion/Server.cs).
- [x] Priority-ordered invoke; unsubscribe; auto-cleanup on `OnDisable`.
- [x] Normalize `ICancellable` on cancellable signals.
- [x] Expose via `IPluginContext.Events`.
- [x] Sample or test plugin subscribes to `PlayerChatSignal`.

### Acceptance

- [x] [03](03-events-priorities.md) §6.

---

## PR 3 — Content registries (Phase 4)

### Implement

- [x] `IContentRegistries` facades wrapping CuratedItemCatalog / BlockRegistry / Commands / Generators.
- [x] Freeze after payloads built; throw on late mutate.
- [x] Migrate MinimalInventoryItems to `ICreativeTabRegistry`.
- [x] Ownership checks + wire to ConflictMode later (or minimal warn).

### Acceptance

- [x] [04](04-registries-content.md) §6.
- [x] Existing curated catalog tests still green.

---

## PR 4 — Services + Messenger (Phase 5)

### Implement

- [x] `ServiceRegistry`, `PluginMessenger`.
- [x] `/plugins` shows provides + soft service resolution.
- [x] Unit tests for TryGet / priority / publish.

### Acceptance

- [x] [05](05-services-messaging.md) §6.

---

## PR 5 — Packet hooks (Phase 6)

### Implement

- [x] Hook points in packet ingress / send path.
- [x] Per-PacketId subscriber maps; `TryOwnHandler`.
- [x] Warn on unfiltered subscribe-all.
- [x] Tests with fake connection or handler counters.

### Acceptance

- [x] [06](06-packet-hooks.md) §6.

---

## PR 6 — Conflicts diagnostics (Phase 7)

### Implement

- [x] `Plugins.ConflictMode`, `IPluginDiagnostics`, rich `/plugins`.
- [x] Emit conflicts from registry/service/packet paths.

### Acceptance

- [x] [07](07-conflicts-compatibility.md) §6.

---

## Suggested agent workflow per PR

1. Read the phase MD (§1–8) + this checklist section.
2. Implement minimal API surface matching sketches.
3. Run targeted `dotnet test` filters.
4. Update sample plugin if API broke.
5. Set phase Status to `implemented` in **both** EN and PT-BR docs.
6. Do not start next PR until acceptance boxes are done.

## Out of scope for all PRs above

- Marketplace / remote plugin install.
- Full custom block RP toolchain.
- Guaranteed multi-plugin merge semantics.
- Native AOT + plugins.

## Quick link map

| Phase | Doc |
|-------|-----|
| Vision | [00](00-vision-minimal-engine.md) |
| Loader | [01](01-loader-contracts-mcmaster.md) |
| Lifecycle | [02](02-lifecycle-manifest.md) |
| Events | [03](03-events-priorities.md) |
| Registries | [04](04-registries-content.md) |
| Services | [05](05-services-messaging.md) |
| Packets | [06](06-packet-hooks.md) |
| Conflicts | [07](07-conflicts-compatibility.md) |
