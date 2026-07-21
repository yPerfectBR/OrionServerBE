# Phase 13 — Events & signals catalog (final)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/13-sdk-events-signals.md`](../../pt_br/plugins/13-sdk-events-signals.md)  
**Depends on:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [03 — Events](03-events-priorities.md)

## 1. Goal

Define the **final typed signal catalog** for deep gameplay: all signals live in `Orion.Api.Events`, expose `IPlayer`/`IEntity`/`IDimension` facades, expand beyond the current twelve `ServerEvent` values, and document emit sites + thread affinity so plugins subscribe via `IEventBus` without referencing `Orion.Events` in the implementation assembly.

## 2. Non-goals

- Guaranteeing cross-area ordering beyond documented affinity.
- Replacing packet ownership for ItemStackRequest (that stays Gameplay.Api / VanillaInventory).
- Async event handlers in v1 of the SDK train.

## 3. Subscription model (unchanged shell)

```csharp
context.Events.Subscribe<PlayerPlaceBlockSignal>(handler, EventPriority.Normal);
```

`IEventBus`, `ISignal`, `ICancellable`, `EventPriority` remain in PluginContracts ([03](03-events-priorities.md)). Signal **classes** move to `Orion.Api.Events`.

## 4. Final signal catalog

### 4.1 Existing (migrate to Orion.Api.Events)

| Signal | Cancellable | Key properties | Emit site (today → keep) |
|--------|:-----------:|----------------|--------------------------|
| `ServerStartSignal` | no | — | Server bootstrap |
| `EntityHurtSignal` | yes | `IEntity Entity`, `float Amount`, cause, `IEntity? Damager` | Damage pipeline |
| `EntitySpawnSignal` | no | `IEntity Entity`, spawn options | Entity spawn |
| `EntityDieSignal` | yes | `IEntity Entity` | Death pipeline |
| `PlayerChatSignal` | yes | `IPlayer Player`, `RawMessage`, `Message` | Chat handler |
| `PlayerJoinSignal` | yes | `IPlayer Player` | Login pipeline |
| `PlayerSpawnSignal` | yes | `IPlayer Player` | First spawn |
| `PlayerLeaveSignal` | no | `IPlayer Player` | Disconnect |
| `PlayerPlaceBlockSignal` | yes | `IPlayer`, `BlockPos`, face | Building / transaction |
| `PlayerBreakBlockSignal` | yes | `IPlayer`, `BlockPos`, face | Mining / transaction |
| `PlayerOpenInventorySignal` | yes | `IPlayer` | VanillaInventory |
| `PlayerOpenContainerSignal` | yes | `IPlayer`, `BlockPos`, container id | VanillaContainerBlocks |

Base types:

```csharp
namespace Orion.Api.Events;

public abstract class ServerSignal : ISignal { }
public abstract class EntitySignal : ServerSignal
{
    public required IEntity Entity { get; init; }
}
public abstract class PlayerSignal : EntitySignal
{
    public IPlayer Player => (IPlayer)Entity;
}
```

### 4.2 New signals (required for deep gameplay)

| Signal | Cancellable | Key properties | Emit site |
|--------|:-----------:|----------------|-----------|
| `PlayerInteractEntitySignal` | yes | `IPlayer`, `IEntity Target`, hand | Use-entity transaction / interact |
| `PlayerItemUseSignal` | yes | `IPlayer`, `IItemStack Item`, air/block target | Item use begin |
| `PlayerItemUseCompleteSignal` | yes | `IPlayer`, `IItemStack`, duration | Item use release / finish |
| `PlayerDropItemSignal` | yes | `IPlayer`, `IItemStack` | Drop path |
| `PlayerPickupItemSignal` | yes | `IPlayer`, `IEntity ItemEntity`, `IItemStack` | Collect path |
| `PlayerContainerCloseSignal` | no | `IPlayer`, window id, `IContainer?` | Container close |
| `PlayerInventorySlotChangeSignal` | no | `IPlayer`, `IContainer`, slot, old/new stacks | After successful set (rate-limited / coalesced docs) |
| `PlayerFoodEatSignal` | yes | `IPlayer`, `IItemStack` | Hunger/eat before apply |
| `PlayerHungerChangeSignal` | no | `IPlayer`, old/new hunger/saturation | After hunger mutate |
| `PlayerGamemodeChangeSignal` | yes | `IPlayer`, old/new gamemode | `SetGamemode` |
| `BlockExplodeSignal` | yes | `IDimension`, positions / cause | Explosion pipeline when present |
| `ChunkLoadSignal` / `ChunkUnloadSignal` | no | `IDimension`, chunk XZ | Chunk lifecycle (for markers / holograms) |

`ServerEvent` enum in PluginContracts expands to include new ids **or** is deprecated in favor of type-only subscribe (preferred final: **type-only**; enum kept for diagnostics mapping in `SignalEventMap`).

## 5. Affinity rules (final)

| Signal family | Thread / affinity |
|---------------|-------------------|
| Player\* / inventory / container | Player’s area / session thread (same as today’s player packet handling) |
| EntityHurt / Die / Spawn | Entity’s area thread |
| ServerStart | Global boot thread |
| ChunkLoad / Unload | Dimension/area thread that owns the chunk |

Document: handlers must not block; long work → schedule on host scheduler APIs when exposed. Handlers that touch another area’s entities must use published marshaling (or only read immutable snapshots).

## 6. Public API sketch — example

```csharp
public void OnEnable(IPluginContext context)
{
    context.Events.Subscribe<PlayerPlaceBlockSignal>(signal =>
    {
        if (signal.BlockPosition.Y > 200)
            signal.Cancel();
    }, EventPriority.High);
}
```

## 7. File touch list

| Path | Change |
|------|--------|
| New `src/Orion.Api/Events/*.cs` | All signal classes |
| Move/delete `src/Orion/Events/**` public signals | Implementation emits Api types |
| `SignalEventMap.cs` | Map new signals |
| `ServerEvent` enum | Extend or diagnostics-only |
| Emit call sites | Join, damage, VanillaInventory, Building, **Mining plugin** (`IServer.Emit`), Attributes, ItemEntity collect, DropItem |

## 8. Acceptance tests

- Third-party plugin cancels `PlayerPlaceBlockSignal`; block is not placed.
- `PlayerFoodEatSignal` cancel prevents hunger change when VanillaAttributes loaded.
- Subscribe by type works across ALC (`typeof(PlayerJoinSignal).Assembly` is Orion.Api).
- No plugin references `Orion.Events` namespace from Orion.dll.

## 9. Migration notes

- Rename properties to facades (`Player` → `IPlayer` already via interface implementation).
- Packet-level features without signals still use [15](15-sdk-protocol-escape.md); new signals above are the supported high-level path.

## 10. Status

`spec`
