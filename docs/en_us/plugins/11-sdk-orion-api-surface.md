# Phase 11 — Orion.Api surface (final facades)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/11-sdk-orion-api-surface.md`](../../pt_br/plugins/11-sdk-orion-api-surface.md)  
**Depends on:** [10 — Packages](10-sdk-packages-versioning.md)

## 1. Goal

Define the **complete final public surface** of `Orion.Api`: facades for server, world, dimension, entity, player, block, item, container, math/value types used by facades, and network helpers that deep plugins need **without** referencing `Orion.dll` or Protocol for common paths.

Concrete Orion types implement these interfaces (or are wrapped). Plugins type only against `Orion.Api` (+ contracts / gameplay api).

## 2. Non-goals

- Exposing area-thread internals (`AreaShard`, `ThreadGuard`) as public API beyond documented affinity rules.
- Full Bedrock creative catalog authoring in this package (see [12](12-sdk-registries-traits.md)).
- Moving McMaster lifecycle types out of PluginContracts.

## 3. Namespaces (final)

| Namespace | Contents |
|-----------|----------|
| `Orion.Api` | `IServer`, `IWorld`, `IDimension`, `IEntity`, `IPlayer` |
| `Orion.Api.Blocks` | `IBlock`, `IBlockType`, `IBlockPermutation`, block helpers |
| `Orion.Api.Items` | `IItemStack`, `IItemType` |
| `Orion.Api.Containers` | `IContainer`, `ContainerType` (moved/aligned from `Orion.Containers`) |
| `Orion.Api.Events` | Typed signal classes (see [13](13-sdk-events-signals.md)) |
| `Orion.Api.Math` | `BlockPos`, `Vec3f` facades or type-forwards stable for plugins |
| `Orion.Api.Network` | `IPacketSender`, high-level send helpers (no raw Protocol required) |

**Naming note:** Empty stubs `IOrionServer` / `IOrionWorld` in PluginContracts are **removed or obsolete**. `IPluginContext.Server` returns `Orion.Api.IServer`. Update PluginContracts facades accordingly in the same ship.

## 4. Public API sketch

### 4.1 Server & world

```csharp
namespace Orion.Api;

public interface IServer
{
    IReadOnlyCollection<IPlayer> OnlinePlayers { get; }
    IWorld? DefaultWorld { get; }
    IWorld? GetWorld(string name);
    IReadOnlyCollection<IWorld> Worlds { get; }
}

public interface IWorld
{
    string Name { get; }
    IDimension? GetDimension(string name);
    IReadOnlyCollection<IDimension> Dimensions { get; }
}
```

| Member | Maps to (implementation) |
|--------|---------------------------|
| `OnlinePlayers` | Enumerate sessions / players on `Server` |
| `GetWorld` / `Worlds` | World manager in Orion host |
| `GetDimension` | Dimension registry on world |

### 4.2 Dimension

```csharp
namespace Orion.Api;

public interface IDimension
{
    string Name { get; }
    IWorld World { get; }

    IBlock? GetBlock(int x, int y, int z, int layer = 0);
    void SetBlock(int x, int y, int z, IBlock block, int layer = 0, bool dirty = true);

    IBlockPermutation GetPermutation(int x, int y, int z, int layer = 0);
    void SetPermutation(int x, int y, int z, IBlockPermutation permutation, int layer = 0, bool dirty = true);

    IEntity SpawnEntity(string typeIdentifier, Vec3f position, EntitySpawnOptions? options = null);
    IReadOnlyCollection<IEntity> GetEntities();

    void Broadcast(IOutboundPacket packet, BroadcastOptions? options = null);
}
```

| Member | Maps to |
|--------|---------|
| `GetBlock` / `SetBlock` | [`DimensionGameplayExtensions`](../../../src/Orion/World/DimensionGameplayExtensions.cs) |
| `GetPermutation` / `SetPermutation` | Same + world palette bridge |
| `SpawnEntity` | `Entity` ctor + `Spawn(Dimension, …)` / scheduling extensions |
| `Broadcast` | `DimensionBroadcastExtensions` |
| `GetEntities` | Dimension entity index |

`IOutboundPacket` is an Orion.Api abstraction; host wraps Protocol `DataPacket`. Plugins that need raw packets use [15](15-sdk-protocol-escape.md).

### 4.3 Entity & player

```csharp
namespace Orion.Api;

public interface IEntity
{
    long UniqueId { get; }
    ulong RuntimeId { get; }
    string TypeIdentifier { get; }
    IDimension? Dimension { get; }
    Vec3f Position { get; }
    bool IsPlayer();
    T? GetTrait<T>() where T : class;
}

public interface IPlayer : IEntity
{
    string Username { get; }
    string Xuid { get; }
    Guid Uuid { get; }
    bool IsOnline { get; }
    bool Spawned { get; }
    bool IsOperator { get; }

    Gamemode Gamemode { get; }
    void SetGamemode(Gamemode gamemode);

    void SendMessage(string message);
    void Disconnect(string reason = "");
    void Teleport(Vec3f position, IDimension? dimension = null, bool forceDimensionChange = false);

    void Send(params IOutboundPacket[] packets);
    void SetHud(HudVisibility visibility, params HudElement[] elements);

    bool DropItem(IItemStack item);
    void SyncInventoryToClient();

    IReadOnlyDictionary<int, IContainer> OpenedContainers { get; }
    void RegisterOpenContainer(int windowId, IContainer container);
    bool TryGetOpenContainer(int windowId, out IContainer? container);

    bool HasPermission(string permission);
}
```

| Member | Maps to [`Player.cs`](../../../src/Orion/Player/Player.cs) / `Entity` |
|--------|------|
| Identity / online | `Username`, `Xuid`, `Uuid`, `Session` |
| Gamemode / permissions | `SetGamemode`, `HasPermission`, … |
| `Send` / `SendMessage` / `SetHud` | Existing methods |
| Containers | `openedContainers`, `RegisterOpenContainer`, `TryGetOpenContainer` |
| `Teleport` / `DropItem` | Existing |
| `GetTrait<T>` | Entity trait system — trait types for plugins live in Orion.Api or plugin-private; first-party traits exposed as interfaces where needed |

**Gamemode / HudVisibility / HudElement:** defined or type-forwarded in Orion.Api (stable enums), not Protocol enums leaked as required dependency.

### 4.4 Blocks

```csharp
namespace Orion.Api.Blocks;

public interface IBlockType
{
    string Identifier { get; }
    bool Solid { get; }
    bool Air { get; }
    float Hardness { get; }
    IReadOnlyList<string> Tags { get; }
}

public interface IBlockPermutation
{
    int NetworkId { get; }
    IBlockType Type { get; }
}

public interface IBlock
{
    IBlockType Type { get; }
    IBlockPermutation Permutation { get; }
    void NotifyBroken(IPlayer breaker, BlockPos blockPosition);
}
```

Maps to [`Block`](../../../src/Orion/Block/Block.cs), [`BlockType`](../../../src/Orion/Block/BlockType.cs), [`BlockPermutation`](../../../src/Orion/Block/BlockPermutation.cs). Trait registration: [12](12-sdk-registries-traits.md).

Factory helpers on Orion.Api:

```csharp
public static class Blocks
{
    public static IBlock Create(string identifier);
    public static IBlock? TryCreate(string identifier);
    public static IBlockPermutation? TryGetDefaultPermutation(string identifier);
}
```

`IServer.Emit(ISignal)` lets gameplay plugins (e.g. `orion:mining`) dispatch cancellable signals such as `PlayerBreakBlockSignal` without referencing Orion.dll internals.

### 4.5 Items

```csharp
namespace Orion.Api.Items;

public interface IItemType
{
    string Identifier { get; }
    int NetworkId { get; }
    int MaxStackSize { get; }
    IReadOnlyList<string> Tags { get; }
    IBlockType? BlockType { get; }
}

public interface IItemStack
{
    IItemType Type { get; }
    int StackSize { get; }
    void SetStackSize(int size);
    bool CanStackWith(IItemStack other);
    IItemStack Clone();
}

public static class Items
{
    public static IItemType? GetType(string identifier);
    public static IItemStack Create(string identifier, int count = 1);
    public static IItemStack Create(IItemType type, int count = 1);
}
```

Maps to [`ItemStack`](../../../src/Orion/Item/ItemStack.cs), [`ItemType`](../../../src/Orion/Item/ItemType.cs).

### 4.6 Containers

Move / align [`IContainer`](../../../src/Orion/Containers/IContainer.cs) and `ContainerType` into `Orion.Api.Containers` (same members; `Show`/`Close` take `IPlayer`):

```csharp
namespace Orion.Api.Containers;

public interface IContainer
{
    ContainerType Type { get; }
    int? Identifier { get; set; }
    int GetSize();
    IItemStack? GetItem(int slot);
    void SetItem(int slot, IItemStack item);
    bool AddItem(IItemStack item);
    void ClearSlot(int slot);
    void Clear();
    void Update();
    void UpdateSlot(int slot);
    int Show(IPlayer player);
    void Close(IPlayer player);
    bool RemoveViewer(IPlayer player, bool sendClose);
    IReadOnlyCollection<KeyValuePair<IPlayer, int>> GetAllOccupants();
}
```

Concrete `Container` class remains in **VanillaContainers** plugin (implements `IContainer`); namespace may stay `Orion.Api.Containers` for the interface only.

### 4.7 Network helpers (no Protocol required)

```csharp
namespace Orion.Api.Network;

public interface IOutboundPacket { }

public static class BlockNetwork
{
    public static IOutboundPacket CreateUpdateBlock(
        BlockPos position,
        IBlockPermutation permutation,
        int layer = 0);
}

public static class ActorNetwork
{
    // high-level helpers as needed for common plugin tasks
}
```

Host maps helpers to Protocol packets internally.

### 4.8 Context wiring

```csharp
// PluginContracts — final
public interface IPluginContext
{
    IPluginManifest Manifest { get; }
    Orion.Api.IServer Server { get; }
    IServiceRegistry Services { get; }
    IPluginMessenger Messenger { get; }
    IEventBus Events { get; }
    IContentRegistries Registries { get; }
    IPacketPipeline Packets { get; }
}

public interface IWorldInitContext
{
    Orion.Api.IWorld World { get; }
    IContentRegistries Registries { get; }
}
```

Signals expose `IPlayer` / `IEntity` instead of concrete `Player` / `Entity` ([13](13-sdk-events-signals.md)).

## 5. Implementation pattern (host)

```csharp
// Orion.dll
sealed class PlayerFacade : IPlayer
{
    private readonly Player _inner;
    public string Username => _inner.Username;
    // …
}

// Or: Player : IPlayer (preferred when binary-compatible)
public sealed class Player : Entity, IPlayer { … }
```

**Preferred final approach:** concrete `Player`, `Entity`, gameplay `Block`, `ItemStack` **implement** Orion.Api interfaces directly so no double-wrap allocation on hot paths. World `Dimension` implements `IDimension` via partial/wrapper if the type lives in `World` project — use an Orion-side adapter registered on spawn.

## 6. File touch list

| Path | Change |
|------|--------|
| `src/Orion.Api/**` | All interfaces above |
| `src/PluginContracts/IPluginContext.cs` | `Server` → `IServer` |
| `src/PluginContracts/IWorldInitContext.cs` | `World` → `IWorld` |
| `src/PluginContracts/Stubs.cs` | Delete empty stubs |
| `src/Orion/Player/Player.cs` | `IPlayer` |
| `src/Orion/Entity/Entity.cs` | `IEntity` |
| `src/Orion/Block/*`, `Item/*` | Implement Api interfaces |
| `src/Orion/Containers/*` | Move types to Orion.Api or type-forward |
| `src/Orion/World/DimensionGameplayExtensions.cs` | Surface via `IDimension` |
| Gameplay services | Take `IPlayer` not `Player` ([14](14-sdk-gameplay-services.md)) |

## 7. Acceptance tests

- External plugin compiles with only Orion.Api PackageReference (plus contracts) and calls `IDimension.SetBlock` / `IPlayer.SendMessage`.
- `player is IPlayer` true for online players.
- VanillaInventory `TryGive(IPlayer, IItemStack, …)` works after dogfood.
- No plugin csproj references `Orion.csproj`.
- Reflection: `typeof(IPlayer).Assembly.GetName().Name == "Orion.Api"`.

## 8. Migration notes

- Replace all plugin usings of `Orion.Player.Player` with `Orion.Api.IPlayer` at API boundaries; cast to concrete only inside Orion.dll.
- `ItemStack` / `IItemStack`: gameplay services and containers use interface; concrete class implements interface.
- Dual block permutation (world vs gameplay) stays an implementation detail behind `IDimension.GetBlock` / `SetBlock`.

## 9. Status

`spec`
