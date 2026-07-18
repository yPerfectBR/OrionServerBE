# Fase 3 — Eventos e prioridades

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/03-events-priorities.md`](../../en_us/plugins/03-events-priorities.md)

## 1. Objetivo

Expor o bus de signals do Orion aos plugins via **contratos**, com **prioridade de handler**, **cancelamento** consistente e **thread affinity** documentada, para inscrição em `OnEnable` (depois do `Server` existir), não no `Load` pré-server.

## 2. Não-objetivos

- Binding por nome `onPlayerJoin` (estilo Serenity) — Orion usa subscribe tipado.
- Distribuição de eventos cross-process.
- Garantir composição quando dois plugins ignoram cancel/priority.

## 3. Esboço de API pública

```csharp
namespace Orion.PluginContracts.Events;

public enum EventPriority
{
    Lowest = 0,
    Low = 1,
    Normal = 2,
    High = 3,
    Highest = 4,
    Monitor = 5  // não muta / não cancela
}

public interface IEventBus
{
    void Subscribe<TSignal>(Action<TSignal> handler, EventPriority priority = EventPriority.Normal)
        where TSignal : ISignal;

    void Unsubscribe<TSignal>(Action<TSignal> handler) where TSignal : ISignal;

    IDisposable SubscribeDisposable<TSignal>(Action<TSignal> handler, EventPriority priority = EventPriority.Normal)
        where TSignal : ISignal;
}

public interface ISignal
{
    ServerEvent Event { get; }
}

public interface ICancellable
{
    bool Cancelled { get; }
    void Cancel();
}
```

Uso:

```csharp
public void OnEnable(IPluginContext ctx)
{
    ctx.Events.Subscribe<PlayerChatSignal>(OnChat, EventPriority.High);
}
```

### Semântica de prioridade

1. Handlers: **Highest → Lowest**, depois **Monitor**.
2. v1: todos ainda são chamados após cancel; handlers devem checar `Cancelled`. v1.1 pode adicionar `ignoreCancelled`.
3. **Monitor** que cancela/muta ⇒ warning; cancel ignorado.

### Thread affinity

Reutilizar [`SignalAffinity`](../../../src/Orion/Scheduling/SignalAffinity.cs): `ServerStart`/`PlayerJoin` globais; eventos de player/entity na area thread quando habilitado.

## 4. Sequência

1. Core cria `Server`.
2. `OnEnable`: `Subscribe`.
3. Sites de `Emit` inalterados; bus despacha por prioridade.
4. `OnDisable`: auto-unsubscribe via contexto.

## 5. Arquivos a tocar

| Path | Mudança |
|------|---------|
| [`Server.cs`](../../../src/Orion/Server.cs) | Listas por prioridade; Off; `IEventBus` |
| [`Events/`](../../../src/Orion/Events/) | `ICancellable` consistente; `ISignal` nos contracts |
| [`SignalAffinity.cs`](../../../src/Orion/Scheduling/SignalAffinity.cs) | Documentar |
| Emit sites | Honrar cancel |

## 6. Testes de aceitação

- Dois handlers em chat: maior prioridade primeiro; cancel impede broadcast.
- Subscribe em `Load` ⇒ erro claro.
- Unsubscribe / disable remove handler.
- Monitor não cancela.
- Affinity de área preservada.

## 7. Notas de migração

| Hoje | Alvo |
|------|------|
| `Server.On<T>` | `IEventBus.Subscribe<T>` com prioridade |
| Sem subscribers | Plugins se registram |
| `Load` antes do Server | Subscribe só em `OnEnable` |

## 8. Status

`implemented`

## Superfície inicial de eventos

`ServerStart`, `PlayerJoin`/`Spawn`/`Leave`/`Chat`, `PlayerPlaceBlock`/`BreakBlock`, `EntityHurt`/`Spawn`/`Die`.

## Eventos entre plugins

Não emitir em eventos C# privados de outro plugin. Usar signals de domínio do core ou Messenger/Services (Fase 5).
