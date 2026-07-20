# Fase 15 — Escape hatch Protocol (final)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/15-sdk-protocol-escape.md`](../../en_us/plugins/15-sdk-protocol-escape.md)  
**Depende de:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [06 — Packet hooks](06-packet-hooks.md)

## 1. Goal

Documentar a política **final**: quando usar Protocol / `IPacketPipeline` versus helpers Orion.Api e serviços Gameplay.Api — sem tornar Protocol shared no McMaster nem dependência obrigatória do SDK.

## 2. Non-goals

- Shared Protocol no ALC por padrão.
- Reimplementar toda a superfície de packets Bedrock em Orion.Api no primeiro train.
- Plugins substituírem codecs do core.

## 3. Árvore de decisão

1. Existe facade / service / sinal? → use isso.  
2. Senão, basta observar/own via `IPacketPipeline`? → hooks.  
3. Senão, `PackageReference Orion.Protocol` e construir packets; enviar via `IPlayer.Send` / `IDimension.Broadcast` (`IOutboundPacket` + adapter do host).

## 4. Regras

- Preferir `BlockNetwork.CreateUpdateBlock`, `SetBlock`, `TryGive`, sinais canceláveis.
- Protocol **não** entra em SharedAssemblies.
- Interfaces públicas de Gameplay.Api **não** expõem `DataPacket`.
- `TryOwnHandler` exclusivo; VanillaInventory dono de ISR / ContainerClose / MobEquipment.

## 5. File touch list

Helpers em `Orion.Api.Network`; Protocol packable; adapter `DataPacket` → `IOutboundPacket`.

## 6. Acceptance tests

- Plugin sem Protocol faz setblock + update via Api.
- Plugin com Protocol envia `UpdateBlockPacket` via adapter.
- Segundo owner de ISR falha.

## 7. Migration notes

- Vanilla\* podem manter Protocol internamente; superfície pública = Api.
- Helper novo em Orion.Api quando o mesmo padrão Protocol se repetir.

## 8. Status

`spec` — **auditoria jul/2026:** `IPacketPipeline` e hooks de packet **implementados** (fase 06); `IOutboundPacket` / helpers em `Orion.Api.Network` **ausentes**; plugins ainda podem referenciar `Protocol` via `Orion.csproj`.
