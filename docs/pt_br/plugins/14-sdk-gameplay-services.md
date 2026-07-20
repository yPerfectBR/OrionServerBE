# Fase 14 — Serviços Orion.Gameplay.Api (final)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/14-sdk-gameplay-services.md`](../../en_us/plugins/14-sdk-gameplay-services.md)  
**Depende de:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [05 — Services](05-services-messaging.md)

## 1. Goal

Mover todos os contratos de domínio de [`src/Orion/Gameplay/`](../../../src/Orion/Gameplay/) para **`Orion.Gameplay.Api`**, tipar com facades `Orion.Api`, documentar `provides` e travar **ownership de packets** por plugin Vanilla.

## 2. Non-goals

- Implementar gameplay vanilla no core.
- Múltiplos owners do mesmo `PacketId`.
- Economy/minigames neste pacote (`Foo.Api`).

## 3. Pacote

`Orion.Gameplay.Api`, namespace `Orion.Gameplay`. Refs: `Orion.Api`, `PluginContracts`.

## 4. API final (resumo)

- `IVanillaInventoryApi` / `IPlayerInventoryService` / `IPlayerInventoryAccess` — parâmetros `IPlayer`, `IItemStack`, `IContainer`.
- `IVanillaBuildingApi` / `IPlayerBlockUseHandler`.
- `IVanillaMiningApi` / `IPlayerBlockBreakHandler`.
- `IPlayerItemUseHandler`.
- `IVanillaAttributesApi` / `IEntityHealthService` / `IPlayerHungerService`.

Registro: `context.Services.Register<…>(…)`. Consumo: `TryGet`.

## 5. Provides e packets

| Plugin | provides | PacketIds exclusivos |
|--------|----------|----------------------|
| VanillaContainers | `orion:containers` | — |
| VanillaInventory | `orion:inventory` | ItemStackRequest, ContainerClose, MobEquipment |
| VanillaBuilding | `orion:building` | — |
| VanillaMining | `orion:mining` | — |
| VanillaAttributes | `orion:attributes`, `orion:health`, `orion:hunger` | — |
| VanillaContainerBlocks | `orion:block-containers` | — |

## 6. File touch list

Criar `src/Orion.Gameplay.Api/**`; remover interfaces de `src/Orion/Gameplay/`; atualizar core + Vanilla\*; SharedAssemblies.

## 7. Acceptance tests

- Terceiro com Gameplay.Api + VanillaInventory: `TryGet` ok.
- Sem VanillaInventory: `TryGet` false.
- Ownership de ISR exclusivo.
- Comando `/give` via service.

## 8. Migration notes

- Namespace `Orion.Gameplay` preservado; assembly `Orion.Gameplay.Api`.
- DTOs de ISR: preferir wrappers no boundary público.

## 9. Status

`spec` — **auditoria jul/2026:** `IInventoryApi`, `IBuildingApi`, `IMiningApi`, `IAttributesApi` **existem** em `Orion.dll` (renomeados de `IVanilla*`); **não** em assembly `Orion.Gameplay.Api`; serviços registrados por plugins `orion:*` via `Services.Register`.
