# Fase 6 — Packet hooks

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/06-packet-hooks.md`](../../en_us/plugins/06-packet-hooks.md)

## 1. Objetivo

Adicionar **hooks de receive/send de pacotes** para um plugin implementar gameplay que o core ainda não expõe (ex.: física de projétil), no espírito Endstone `PacketReceiveEvent`/`PacketSendEvent` e PocketMine `DataPacketReceiveEvent`.

É **escape hatch**, não a API padrão.

## 2. Não-objetivos

- Garantia estável do layout raw em todo bump de protocolo sem versionar.
- Plugin substituir a stack de rede inteira.
- Abstração zero-custo — hot path; opt-in por PacketId.

## 3. Esboço de API pública

```csharp
namespace Orion.PluginContracts.Network;

public interface IPacketPipeline
{
    void OnReceive(PacketHook hook);
    void OnSend(PacketHook hook);
    bool TryOwnHandler(int packetId, IOrionPlugin owner, PacketHandlerDelegate handler);
}

public sealed class PacketReceiveContext : ICancellable
{
    public required IPlayerConnection Connection { get; init; }
    public required int PacketId { get; init; }
    public required ReadOnlyMemory<byte> Payload { get; init; }
    public bool Handled { get; set; }
    // Cancel() ...
}
```

### Preferir eventos de alto nível

| Necessidade | Preferir |
|-------------|----------|
| Moderação de chat | `PlayerChatSignal` |
| Quebra de bloco | `PlayerBreakBlockSignal` |
| Sync de projétil custom | Packet hooks + futuros signals |
| Pacote novo do cliente | Hooks até codec + evento no core |

### Exemplo: plugin de projétil

1. `TryOwnHandler` / `OnReceive` filtrado.
2. Simulação no plugin.
3. Envio de packets de entidade.
4. Migrar quando o core tiver `ProjectileLaunchSignal`.

## 4. Sequência

Inbound: unframe → hooks → se Cancel drop; se Handled skip switch; senão handlers core.  
Outbound: hooks → cancel/replace → write.

## 5. Arquivos a tocar

[`Network/`](../../../src/Orion/Network/) PacketIngress / NetworkHandler; `PacketPipeline`; expor em `IPluginContext`; early-out sem subscribers.

## 6. Testes de aceitação

- Cancel impede handler core.
- Segundo `TryOwnHandler` rejeitado.
- Subscribe-all gera warning.
- Sem subscribers ≈ overhead zero.
- Payload versionado ao `Raknet.Protocol` do config.

## 7. Notas de migração

Hoje: `switch` fechado. Fase 6 abre o pipe sem remover handlers core.

## 8. Status

`implemented`

Payloads de hooks são específicos da versão de protocolo configurada (`Raknet.Protocol` / codec do host).

## Regras de segurança e performance

1. Sempre filtrar por `PacketId`.
2. Evitar alloc por packet em Monitor.
3. Não bloquear threads de área/sessão com I/O.
4. Preferir tipos de packet do core quando existirem.
5. Plugin pesado pode travar o server — modelo de confiança do operador.
