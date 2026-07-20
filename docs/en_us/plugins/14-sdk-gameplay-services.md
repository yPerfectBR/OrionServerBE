# Phase 14 — Orion.Gameplay.Api services (final)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/14-sdk-gameplay-services.md`](../../pt_br/plugins/14-sdk-gameplay-services.md)  
**Depends on:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [05 — Services](05-services-messaging.md)

## 1. Goal

Move all domain gameplay contracts from [`src/Orion/Gameplay/`](../../../src/Orion/Gameplay/) into published **`Orion.Gameplay.Api`**, type them against `Orion.Api` facades (`IPlayer`, `IItemStack`, `IContainer`), document `provides` capabilities, and lock **packet ownership** per Vanilla plugin for the final architecture.

## 2. Non-goals

- Implementing vanilla gameplay inside the core.
- Multiple owners of the same `PacketId` (still exclusive `TryOwnHandler`).
- Embedding Economy/minigame APIs here (those are `Foo.Api`).

## 3. Package & namespaces

| Package | Namespace |
|---------|-----------|
| `Orion.Gameplay.Api` | `Orion.Gameplay` (keep familiar names) |

Project references: `Orion.Api`, `Orion.PluginContracts`.

All interfaces currently under `src/Orion/Gameplay/*.cs` **move** to this project; Orion.dll and Vanilla\* reference the NuGet/project.

## 4. Public API sketch (final signatures)

### Inventory

```csharp
namespace Orion.Gameplay;

public interface IInventoryApi
{
    IPlayerInventoryService Inventory { get; }
}

public interface IPlayerInventoryService
{
    bool TryOpenInventory(IPlayer player);
    bool TryCloseInventory(IPlayer player, int windowId);
    bool TryGetAccess(IPlayer player, out IPlayerInventoryAccess? access);
    IItemStack? GetHeldItem(IPlayer player);
    bool TrySetHeldSlot(IPlayer player, int slot);
    bool TryGive(IPlayer player, IItemStack stack, out int leftover);
    bool TryClear(IPlayer player);
    bool TryCollect(IPlayer player, IItemStack stack, out ushort moved);
    bool TrySyncToClient(IPlayer player);
    void EnableHud(IPlayer player);
    IContainer? ResolveContainer(IPlayer player, FullContainerName name);
    bool TryProcessItemStackRequest(IPlayer player, ItemStackRequest request, out ItemStackResponse response);
}

public interface IPlayerInventoryAccess
{
    IContainer Container { get; }
    int SelectedSlot { get; }
    void SetHeldSlot(int slot);
    IItemStack? GetHeldItem();
    void Clear();
    void SyncToPlayer(IPlayer player);
    void SyncHeldItemToClient(IPlayer player);
}
```

`FullContainerName`, `ItemStackRequest`, `ItemStackResponse`: either facades in Orion.Api / Gameplay.Api or documented Protocol types used only by inventory owners ([15](15-sdk-protocol-escape.md)). Prefer thin DTOs in Gameplay.Api for request/response so most consumers avoid Protocol.

### Building / mining / item use

```csharp
public interface IBuildingApi
{
    IPlayerBlockUseHandler BlockUse { get; }
}

public interface IPlayerBlockUseHandler
{
    bool TryUseOnBlock(IPlayer player, BlockPos blockPos, int face, BlockPos placePos, IItemStack? held);
    bool TryUseOnAir(IPlayer player, IItemStack? held);
}

public interface IMiningApi
{
    IPlayerBlockBreakHandler BlockBreak { get; }
}

public interface IPlayerBlockBreakHandler
{
    void OnStartDestroy(IPlayer player, BlockPos pos, int face, ulong tick);
    void OnContinueDestroy(IPlayer player, BlockPos pos, int face, ulong tick);
    void OnCrack(IPlayer player, BlockPos pos, int face, ulong tick);
    void OnAbortDestroy(IPlayer player, BlockPos pos, int face);
    void OnPredictDestroy(IPlayer player, BlockPos pos, int face, ulong tick);
    void OnCreativeDestroy(IPlayer player, BlockPos pos, int face);
}

public interface IPlayerItemUseHandler
{
    bool TryBeginUse(IPlayer player, IItemStack item);
    bool TryCompleteUse(IPlayer player, IItemStack item);
}
```

### Attributes

```csharp
public interface IAttributesApi
{
    IEntityHealthService Health { get; }
    IPlayerHungerService Hunger { get; }
    void EnableHud(IPlayer player);
}

public interface IEntityHealthService
{
    bool TryApplyDamage(IEntity entity, float amount, /* cause */);
    bool TryHeal(IEntity entity, float amount);
    bool TryGet(IEntity entity, out float health);
    bool TrySet(IEntity entity, float health);
}

public interface IPlayerHungerService
{
    bool TryEat(IPlayer player, IItemStack food);
    bool TryAddExhaustion(IPlayer player, float exhaustion);
    bool TryGet(IPlayer player, out int hunger, out float saturation);
    bool TrySetHunger(IPlayer player, int hunger, float saturation);
}
```

### Registration pattern (Vanilla plugin OnEnable)

```csharp
context.Services.Register<IInventoryApi>(services, this);
context.Services.Register<IPlayerInventoryService>(services, this);
```

Consumers:

```csharp
if (context.Services.TryGet(out IPlayerInventoryService? inv) && inv is not null)
    inv.TryGive(player, Items.Create("minecraft:diamond", 1)!, out _);
```

## 5. Replacement policy

| Mechanism | Rule |
|-----------|------|
| Service capability | One owner via `provides` + highest `ServicePriority` on `Register<T>` |
| PacketId | `TryOwnHandler` — first plugin wins; no stealing from `orion:inventory` ISR |
| Open inventory | Cancel `PlayerOpenInventorySignal` or replace `IPlayerInventoryService` without loading `orion:inventory` |
| Hard `depend` | Functional requirement + fixed load order — no `load` field ([19](19-manifest-v2.md)) |

## 6. Provides & packet ownership (final)

| Plugin id | `provides` | Owns PacketIds (exclusive) |
|-----------|------------|----------------------------|
| `orion:containers` | `orion:containers` | — (runtime library) |
| `orion:inventory` | `orion:inventory` | `ItemStackRequest`, `ContainerClose`, `MobEquipment` |
| `orion:building` | `orion:building` | — (driven from InventoryTransaction core → service) |
| `orion:mining` | `orion:mining` | — (driven from player action packets → service) |
| `orion:attributes` | `orion:attributes`, `orion:health`, `orion:hunger` | — |
| `orion:block-containers` | `orion:block-containers` | — |

Core packet handlers call `PluginHost.Services.TryGet<IPlayerBlockUseHandler>()` etc. — unchanged pattern, types from Gameplay.Api.

Conflict if two plugins `TryOwnHandler` the same id: diagnostics per [07](07-conflicts-compatibility.md); first owner wins.

## 6. File touch list

| Path | Change |
|------|--------|
| `src/Orion.Gameplay.Api/**` | All interfaces |
| Delete/empty `src/Orion/Gameplay/` | Types moved |
| Orion + Vanilla\* usings | `Orion.Gameplay` from new assembly |
| Signatures | `Player` → `IPlayer`, `ItemStack` → `IItemStack` |
| SharedAssemblies | Include `Orion.Gameplay.Api` ([10](10-sdk-packages-versioning.md)) |

## 7. Acceptance tests

- External plugin PackageReferences Gameplay.Api only (+ Api/Contracts), `TryGet<IPlayerInventoryService>` succeeds when VanillaInventory loaded.
- External plugin without VanillaInventory: `TryGet` false; no throw.
- VanillaInventory owns the three PacketIds; second plugin claiming `ItemStackRequest` fails ownership.
- Core `Give` command still works via service.

## 8. Migration notes

- Namespace stays `Orion.Gameplay` to minimize churn; assembly name is `Orion.Gameplay.Api`.
- ISR DTOs: if still Protocol types internally, wrap at the service boundary for public consumers where feasible.

## 9. Status

`spec`
