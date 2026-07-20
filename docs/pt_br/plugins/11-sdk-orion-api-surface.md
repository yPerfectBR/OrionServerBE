# Fase 11 — Superfície Orion.Api (facades finais)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/11-sdk-orion-api-surface.md`](../../en_us/plugins/11-sdk-orion-api-surface.md)  
**Depende de:** [10 — Pacotes](10-sdk-packages-versioning.md)

## 1. Goal

Definir a **superfície pública final completa** de `Orion.Api`: facades de server, world, dimension, entity, player, block, item, container, tipos de valor e helpers de rede que plugins deep precisam **sem** referenciar `Orion.dll` nem Protocol nos caminhos comuns.

## 2. Non-goals

- Expor internals de area-thread além das regras de affinity documentadas.
- Authoring completo do catálogo criativo Bedrock neste pacote (ver [12](12-sdk-registries-traits.md)).
- Mover lifecycle McMaster para fora de PluginContracts.

## 3. Namespaces (finais)

| Namespace | Conteúdo |
|-----------|----------|
| `Orion.Api` | `IServer`, `IWorld`, `IDimension`, `IEntity`, `IPlayer` |
| `Orion.Api.Blocks` | `IBlock`, `IBlockType`, `IBlockPermutation` |
| `Orion.Api.Items` | `IItemStack`, `IItemType` |
| `Orion.Api.Containers` | `IContainer`, `ContainerType` |
| `Orion.Api.Events` | Sinais tipados ([13](13-sdk-events-signals.md)) |
| `Orion.Api.Math` | `BlockPos`, `Vec3f` estáveis |
| `Orion.Api.Network` | `IOutboundPacket`, helpers de envio |

Stubs `IOrionServer` / `IOrionWorld` em PluginContracts são **removidos**. `IPluginContext.Server` retorna `Orion.Api.IServer`.

## 4. Public API sketch

### Server / World / Dimension

```csharp
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

Mapeia para [`DimensionGameplayExtensions`](../../../src/Orion/World/DimensionGameplayExtensions.cs), spawn/broadcast existentes.

### Entity / Player

```csharp
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

Mapeia para [`Player.cs`](../../../src/Orion/Player/Player.cs).

### Blocks / Items / Containers

Interfaces `IBlock` / `IBlockType` / `IBlockPermutation`, `IItemStack` / `IItemType`, factories `Blocks.*` / `Items.*`, e `IContainer` alinhado ao atual [`IContainer`](../../../src/Orion/Containers/IContainer.cs) com `IPlayer` / `IItemStack`. Implementação concreta de storage continua em VanillaContainers.

### Network helpers

```csharp
public static class BlockNetwork
{
    public static IOutboundPacket CreateUpdateBlock(BlockPos position, IBlockPermutation permutation, int layer = 0);
}
```

### Context

```csharp
public interface IPluginContext
{
    Orion.Api.IServer Server { get; }
    // Services, Messenger, Events, Registries, Packets…
}
```

## 5. Padrão de implementação (host)

Preferência final: tipos concretos (`Player`, `Entity`, `ItemStack`, …) **implementam** as interfaces Orion.Api diretamente (sem double-wrap no hot path).

## 6. File touch list

`src/Orion.Api/**`, update PluginContracts contexts, `Player`/`Entity`/`Block`/`Item` implementam interfaces, containers movem/alinham, services usam `IPlayer` ([14](14-sdk-gameplay-services.md)).

## 7. Acceptance tests

- Plugin externo compila só com NuGets e usa `SetBlock` / `SendMessage`.
- `player is IPlayer` true.
- `typeof(IPlayer).Assembly` = `Orion.Api`.
- Nenhum plugin referencia `Orion.csproj`.

## 8. Migration notes

- Fronteiras de API usam `IPlayer` / `IItemStack`; concrete só dentro de Orion.dll.
- Dual permutation mundo/gameplay fica atrás de `IDimension`.

## 9. Status

`spec` — **auditoria jul/2026:** facades `IServer`/`IWorld`/`IPlayer` em `Orion.Api` **ausentes**; `IPluginContext` ainda expõe stubs em PluginContracts; tipos de gameplay (`Player`, `Entity`) **não implementam** interfaces Api públicas.
