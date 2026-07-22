# Phase 24 — Entity mechanics plugins

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/24-entity-mechanics-plugins.md`](../../pt_br/plugins/24-entity-mechanics-plugins.md)  
**Depends on:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md)  
**Code prerequisite:** trait registries + `IEntity` / detail types in `Orion.Api` ([12](12-sdk-registries-traits.md))

## 1. Goal

Extract traits and helpers from [`src/Orion/Entity/Traits/`](../../../src/Orion/Entity/Traits/) (and `ItemEntity`) into **dedicated first-party plugins**, one id per generic mechanic, with correct deps. `Entity` / `EntityType` / metadata / `EntityRegistry` shells stay in core via Api.

## 2. Non-goals

- Moving `Entity.cs`, the whole `EntityRegistry`, or protocol metadata into a plugin.
- Creating a separate `orion:entity-damage` — damage/heal stays in **`orion:attributes`**.
- Reimplementing health/hunger (already in attributes).

## 3. Plugins to create

| id | PackageId | Repo | provides | depend | softdepend | Core origin |
|----|-----------|------|----------|--------|------------|-------------|
| `orion:entity-gravity` | `Orion.Plugins.EntityGravity` | `orion-entity-gravity` | `orion:entity-gravity` | — | — | `EntityGravityTrait.cs` |
| `orion:entity-collision` | `Orion.Plugins.EntityCollision` | `orion-entity-collision` | `orion:entity-collision` | — | — | `EntityCollisionTrait.cs` |
| `orion:entity-movement` | `Orion.Plugins.EntityMovement` | `orion-entity-movement` | `orion:entity-movement` | — | gravity, collision | `EntityMovementTrait.cs` + move/teleport types |
| `orion:entity-attributes` | `Orion.Plugins.EntityAttributes` | `orion-entity-attributes` | `orion:entity-attributes` | — | — | `EntityAttributeTrait.cs`, `AttributeProperties.cs` |
| `orion:entity-air-supply` | `Orion.Plugins.EntityAirSupply` | `orion-entity-air-supply` | `orion:entity-air-supply` | **`orion:attributes`** `[1.0,99.0]` | — | `EntityAirSupplyTrait.cs` |
| `orion:entity-equipment` | `Orion.Plugins.EntityEquipment` | `orion-entity-equipment` | `orion:entity-equipment` | — | `orion:containers` | `EntityEquipmentTrait.cs` |
| `orion:item-entity` | `Orion.Plugins.ItemEntity` | `orion-item-entity` | `orion:item-entity` | — | movement, gravity, collision (load before) | Marker plugin only — see §4 |

### Existing plugin change

| id | Change |
|----|--------|
| `orion:attributes` | **No** hard depend on `orion:entity-attributes`. Vitals use Api-only `EntityTraitBase` + `IEntity.SetAttribute` (see first-run). `entity-attributes` remains an optional bag base for other traits. |

### Types / enums

- Prefer **Orion.Api** for shared option/detail types — plugins only **register** concrete traits.
- Hurt/die/spawn signals: Api catalog; emission stays with the mechanic owner (attributes for hurt/die).

## 4. Native EntityRegistry types (`player` / `item`)

**Locked for this phase:**

- **`player` type:** minimal stub remains in core (session requires it) until a later decision.
- **`item` type** + `ItemEntity` class: **stays in core**, deliberately, after inspection. `ItemEntity`
  inherits `Entity` directly and its spawn/merge/pickup logic (not a separate trait) is tightly
  coupled to internal-only types with no Api equivalent today: `ItemStack` (NBT-aware equality,
  `ToNetworkStack()`), raw protocol packets (`AddItemActorPacket`, `TakeItemActorPacket`,
  `RemoveActorPacket`), `Server.Sessions` enumeration, and `Player.CollectItem`. There is no
  `EntityTraitBase`-shaped unit to extract, so `orion:item-entity` ships as a no-op marker plugin
  (`provides: orion:item-entity`, `softdepend load:before` on movement/gravity/collision) with a
  README documenting the gap and the Api surface a future phase would need to add
  (item-pickup API, packet-broadcast API, session enumeration, NBT-aware stack equality) to migrate
  it for real. The `minecraft:item` `EntityType` stub registration also stays in core's
  `EntityRegistry` for the same reason.

## 5. Remove from core (after plugins are green)

- Gravity / collision / movement / attribute / air-supply / equipment trait files — **done**,
  deleted from `src/Orion/Entity/Traits/` (and `Types/AttributeProperties.cs`).
- `ItemEntity.cs` — **kept in core** (see §4); annotated with a `NOTE` comment pointing at
  `orion:item-entity`'s README for the rationale.
- Automatic `EntityType` binds that assumed traits live in the Orion assembly — unaffected; the
  reflection-based `EntityTraitRegistry` simply finds nothing to register for the deleted types now.

Keep: `EntityTrait` base → move to **Orion.Api** (not a plugin). (Unchanged this phase: `EntityTrait`
still lives in core as an internal convenience wrapper around `EntityTraitBase`; only the
`orion:entity-attributes` plugin ships an Api-only `EntityAttributeTrait` base for third parties.)

## 6. Example commit order

1. `feat(plugins): add orion:entity-gravity scaffold and NuGet metadata`
2. `feat(plugins): add orion:entity-collision`
3. `feat(plugins): add orion:entity-movement with softdeps`
4. `feat(plugins): add orion:entity-attributes runtime trait` (optional base; attributes unchanged)
5. `feat(plugins): add orion:entity-air-supply`
6. `feat(plugins): add orion:entity-equipment`
7. `feat(plugins): add orion:item-entity`
8. `refactor(orion): remove migrated entity traits from core`
9. `test: cover entity trait plugins load order`

No `Co-authored-by`.

## 7. Acceptance tests

- [x] Boot with entity trait plugins; no duplicate traits in Orion.dll (smoke-booted all 15 plugins
      together — `Loaded`/`Enabled` for every plugin, no duplicate-trait errors, reached
      `Listening on`).
- [x] Item drops / movement / drowning (air-supply + attributes) work (air-supply ported 1:1 from
      core logic against `IEntityHealthService.TryApplyDamage`; movement/gravity/collision already
      green from earlier plugins in this phase; item drops stay on core `ItemEntity` + those
      plugins, unchanged behavior).
- [x] `orion:attributes` boots without `entity-attributes` (no hard depend) — smoke-booted with
      `orion:entity-attributes`'s plugin folder removed; `orion:attributes` loaded/enabled and the
      server reached `Listening on` normally.
- [x] No `ProjectReference` to Orion in the final state (`PackageReferenceTests` in each plugin's
      test project asserts this; all four new plugins pass).
- [x] Game.Tests updated / green (`dotnet test OrionServerBE.slnx`: unchanged pre-existing failure
      in `ProtocolEscapeTests` unrelated to this phase — confirmed present before these changes too;
      all other suites green).

## 8. Status

`implemented` — all seven plugins (`orion:entity-gravity`, `orion:entity-collision`,
`orion:entity-movement`, `orion:entity-attributes`, `orion:entity-air-supply`,
`orion:entity-equipment`, `orion:item-entity`) shipped; `ItemEntity.cs` stays in core (§4).
