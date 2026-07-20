# Fase 13 — Catálogo de events e sinais (final)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/13-sdk-events-signals.md`](../../en_us/plugins/13-sdk-events-signals.md)  
**Depende de:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [03 — Events](03-events-priorities.md)

## 1. Goal

Definir o **catálogo final de sinais tipados** para gameplay profundo: classes em `Orion.Api.Events`, facades `IPlayer`/`IEntity`, expansão além dos doze `ServerEvent` atuais, emit sites e affinity documentados.

## 2. Non-goals

- Ordenação cross-area além da affinity documentada.
- Substituir ownership de ItemStackRequest (fica Gameplay.Api / VanillaInventory).
- Handlers async no primeiro train do SDK.

## 3. Modelo de subscribe

```csharp
context.Events.Subscribe<PlayerPlaceBlockSignal>(handler, EventPriority.Normal);
```

Shell (`IEventBus`, etc.) permanece em PluginContracts. Classes de sinal em `Orion.Api.Events`.

## 4. Catálogo final

### Existentes (migrar)

`ServerStartSignal`, `EntityHurtSignal`, `EntitySpawnSignal`, `EntityDieSignal`, `PlayerChatSignal`, `PlayerJoinSignal`, `PlayerSpawnSignal`, `PlayerLeaveSignal`, `PlayerPlaceBlockSignal`, `PlayerBreakBlockSignal`, `PlayerOpenInventorySignal`, `PlayerOpenContainerSignal` — propriedades com facades; emit sites atuais preservados.

Bases: `ServerSignal` / `EntitySignal` / `PlayerSignal` em Orion.Api.

### Novos (obrigatórios)

| Sinal | Cancelável | Uso |
|-------|:----------:|-----|
| `PlayerInteractEntitySignal` | sim | Interact em entidade |
| `PlayerItemUseSignal` / `PlayerItemUseCompleteSignal` | sim | Uso de item |
| `PlayerDropItemSignal` / `PlayerPickupItemSignal` | sim | Drop / collect |
| `PlayerContainerCloseSignal` | não | Fechar container |
| `PlayerInventorySlotChangeSignal` | não | Mudança de slot |
| `PlayerFoodEatSignal` / `PlayerHungerChangeSignal` | sim / não | Comida / fome |
| `PlayerGamemodeChangeSignal` | sim | Mudança de gamemode |
| `BlockExplodeSignal` | sim | Explosão |
| `ChunkLoadSignal` / `ChunkUnloadSignal` | não | Lifecycle de chunk |

Subscribe **por tipo** é o path suportado; enum `ServerEvent` fica para diagnostics/`SignalEventMap`.

## 5. Affinity

Player/inventory → thread da área/sessão do player; Entity hurt/die/spawn → área da entidade; ServerStart → boot global; Chunk\* → thread da dimensão/área.

## 6. Exemplo

```csharp
context.Events.Subscribe<PlayerPlaceBlockSignal>(s =>
{
    if (s.BlockPosition.Y > 200) s.Cancel();
}, EventPriority.High);
```

## 7. File touch list

`Orion.Api/Events/*`; emitir a partir do core/Vanilla\*; atualizar `SignalEventMap`; remover dependência pública de `Orion.Events` para plugins.

## 8. Acceptance tests

- Cancel de place impede bloco.
- Cancel de eat impede fome.
- Assembly do sinal = Orion.Api.
- Plugin não referencia namespace de implementação `Orion.Events`.

## 9. Migration notes

- Features sem sinal → [15](15-sdk-protocol-escape.md).
- Path alto nível = sinais desta lista.

## 10. Status

`spec`
