# Fase 24 — Plugins de mecânica de Entity

**Status:** `implemented`  
**Language twin:** [`../../en_us/plugins/24-entity-mechanics-plugins.md`](../../en_us/plugins/24-entity-mechanics-plugins.md)  
**Depende de:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md)  
**Pré-req código:** trait registries + `IEntity` / detail types em `Orion.Api` ([12](12-sdk-registries-traits.md))

## 1. Goal

Extrair traits e tipos auxiliares de [`src/Orion/Entity/Traits/`](../../../src/Orion/Entity/Traits/) (e `ItemEntity`) para **plugins first-party dedicados**, um id por mecânica genérica, com deps corretas. Shell `Entity` / `EntityType` / metadata / `EntityRegistry` (vazio ou mínimo) permanece no core via Api.

## 2. Non-goals

- Mover `Entity.cs`, `EntityRegistry` inteiro, ou metadata de protocolo para plugin.
- Criar `orion:entity-damage` separado — dano/heal permanece em **`orion:attributes`**.
- Reimplementar health/hunger (já em attributes).

## 3. Plugins a criar

| id | PackageId | Repo | provides | depend | softdepend | Origem core |
|----|-----------|------|----------|--------|------------|-------------|
| `orion:entity-gravity` | `Orion.Plugins.EntityGravity` | `orion-entity-gravity` | `orion:entity-gravity` | — | — | `EntityGravityTrait.cs` |
| `orion:entity-collision` | `Orion.Plugins.EntityCollision` | `orion-entity-collision` | `orion:entity-collision` | — | — | `EntityCollisionTrait.cs` |
| `orion:entity-movement` | `Orion.Plugins.EntityMovement` | `orion-entity-movement` | `orion:entity-movement` | — | gravity, collision | `EntityMovementTrait.cs` + Types move/teleport/rendered/fall |
| `orion:entity-attributes` | `Orion.Plugins.EntityAttributes` | `orion-entity-attributes` | `orion:entity-attributes` | — | — | `EntityAttributeTrait.cs`, `AttributeProperties.cs` |
| `orion:entity-air-supply` | `Orion.Plugins.EntityAirSupply` | `orion-entity-air-supply` | `orion:entity-air-supply` | **`orion:attributes`** `[1.0,99.0]` | — | `EntityAirSupplyTrait.cs` (usa `IEntityHealthService`) |
| `orion:entity-equipment` | `Orion.Plugins.EntityEquipment` | `orion-entity-equipment` | `orion:entity-equipment` | — | `orion:containers` | `EntityEquipmentTrait.cs` |
| `orion:item-entity` | `Orion.Plugins.ItemEntity` | `orion-item-entity` | `orion:item-entity` | — | movement, gravity, collision (load before) | Plugin marcador apenas — ver §4 |

### Ajuste em plugin existente

| id | Mudança |
|----|---------|
| `orion:attributes` | **Sem** hard depend em `orion:entity-attributes`. Vitals usam `EntityTraitBase` + `IEntity.SetAttribute` (ver first-run). `entity-attributes` permanece base opcional para outros traits. |

### Tipos / enums

- `EntityInteractMethod`, `EntitySpawnOptions`, `EntityDeathOptions`, `EntityDespawnOptions`, etc.: preferir **Orion.Api** (contratos), não duplicar em cada plugin. Plugins só **registram** traits concretos.
- Sinais `EntitySpawnSignal` / `EntityHurtSignal` / `EntityDieSignal`: catálogo Api ([13](13-sdk-events-signals.md)); emissão continua no dono da mecânica (attributes para hurt/die).

## 4. EntityRegistry nativos (`player` / `item`)

**Decisão travada nesta fase:**

- Tipo **`player`**: stub mínimo permanece no core (sessão exige player type) até fase player/content decidir o contrário.
- Tipo **`item`** + classe `ItemEntity`: **permanecem no core**, decisão deliberada após inspeção.
  `ItemEntity` herda `Entity` diretamente e sua lógica de spawn/merge/pickup (que não é um trait
  separado) está fortemente acoplada a tipos internos sem equivalente Api hoje: `ItemStack`
  (igualdade NBT-aware, `ToNetworkStack()`), pacotes de protocolo brutos (`AddItemActorPacket`,
  `TakeItemActorPacket`, `RemoveActorPacket`), enumeração de `Server.Sessions` e
  `Player.CollectItem`. Não há nenhuma unidade no formato `EntityTraitBase` para extrair, então
  `orion:item-entity` é publicado como plugin marcador no-op (`provides: orion:item-entity`,
  `softdepend load:before` em movement/gravity/collision) com README documentando o gap e a
  superfície de Api que uma fase futura precisaria adicionar (Api de pickup de item, Api de
  broadcast de pacote, enumeração de sessão, igualdade NBT-aware de stack) para migrá-lo de fato.
  O registro stub do `EntityType` `minecraft:item` também permanece no `EntityRegistry` do core pelo
  mesmo motivo.

## 5. Remover do core (após plugins verdes)

- `Entity/Traits/EntityGravityTrait.cs`
- `EntityCollisionTrait.cs`, `EntityMovementTrait.cs`, `EntityAttributeTrait.cs`, `EntityAirSupplyTrait.cs`, `EntityEquipmentTrait.cs` — **feito**, deletados de
  `src/Orion/Entity/Traits/` (e `Types/AttributeProperties.cs`).
- `EntityTraitRegistry.RegisterFromAssembly` para esses tipos — sem impacto; o registro
  reflection-based simplesmente não encontra mais nada para registrar desses tipos deletados.
- `ItemEntity.cs` — **permanece no core** (ver §4); anotado com comentário `NOTE` apontando para o
  README de `orion:item-entity` com a justificativa.
- Bind automático no `EntityType` que assumia traits no assembly Orion — sem impacto.

Manter: `EntityTrait.cs` base → mover para **Orion.Api** (não plugin). (Inalterado nesta fase:
`EntityTrait` continua no core como wrapper interno de conveniência sobre `EntityTraitBase`; só o
plugin `orion:entity-attributes` publica uma base `EntityAttributeTrait` Api-only para terceiros.)

## 6. Ordem de commits (exemplo)

1. `feat(plugins): add orion:entity-gravity scaffold and NuGet metadata`
2. `feat(plugins): add orion:entity-collision`
3. `feat(plugins): add orion:entity-movement with softdeps`
4. `feat(plugins): add orion:entity-attributes runtime trait` (base opcional; attributes inalterado)
5. `feat(plugins): add orion:entity-air-supply`
6. `feat(plugins): add orion:entity-equipment`
7. `feat(plugins): add orion:item-entity`
8. `refactor(orion): remove migrated entity traits from core`
9. `test: cover entity trait plugins load order`

Sem `Co-authored-by`. Push `development` → PR → `main` (publish auto se sources mudarem).

## 7. Acceptance tests

- [x] Boot com plugins de entity traits; sem traits duplicados no Orion.dll (smoke-boot com os 15
      plugins juntos — `Loaded`/`Enabled` para todos, sem erro de trait duplicado, chegou em
      `Listening on`).
- [x] Item drops / movement / drowning (air-supply + attributes) funcionam (air-supply portado 1:1
      da lógica do core contra `IEntityHealthService.TryApplyDamage`; movement/gravity/collision já
      verdes de plugins anteriores nesta fase; item drops continuam no `ItemEntity` do core + esses
      plugins, comportamento inalterado).
- [x] `orion:attributes` sobe sem `entity-attributes` (sem hard depend) — smoke-boot com a pasta do
      plugin `orion:entity-attributes` removida; `orion:attributes` carregou/habilitou normalmente
      e o servidor chegou em `Listening on`.
- [x] Nenhum `ProjectReference` Orion no estado final (`PackageReferenceTests` em cada projeto de
      teste dos plugins garante isso; os quatro novos plugins passam).
- [x] Game.Tests ajustados / verdes (`dotnet test OrionServerBE.slnx`: falha pré-existente em
      `ProtocolEscapeTests` não relacionada a esta fase — confirmada presente antes destas
      mudanças também; demais suites verdes).

## 8. Status

`implemented` — os sete plugins (`orion:entity-gravity`, `orion:entity-collision`,
`orion:entity-movement`, `orion:entity-attributes`, `orion:entity-air-supply`,
`orion:entity-equipment`, `orion:item-entity`) publicados; `ItemEntity.cs` permanece no core (§4).
