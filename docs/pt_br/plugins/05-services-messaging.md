# Fase 5 — Services e messaging

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/05-services-messaging.md`](../../en_us/plugins/05-services-messaging.md)

## 1. Objetivo

Permitir integração **sem hard load dependencies**: descobrir APIs opcionais em runtime (Services) e trocar mensagens em canais namespaced (Messenger). Plugin A sobe mesmo se B estiver ausente.

## 2. Não-objetivos

- RPC obrigatório com protobuf na v1.
- Ordem de entrega cross-thread sem documentar affinity.
- Interfaces de terceiros dentro de `Orion.PluginContracts` (ficam em `Foo.Api`).

## 3. Esboço de API pública

### Services (análogo ao ServicesManager)

```csharp
namespace Orion.PluginContracts.Services;

public enum ServicePriority { Lowest, Low, Normal, High, Highest }

public interface IServiceRegistry
{
    void Register<TService>(TService instance, IOrionPlugin owner, ServicePriority priority = ServicePriority.Normal)
        where TService : class;

    void UnregisterAll(IOrionPlugin owner);

    bool TryGet<TService>(out TService? service) where TService : class;

    TService GetRequired<TService>() where TService : class;
}
```

### Messenger

```csharp
namespace Orion.PluginContracts.Messaging;

public interface IPluginMessenger
{
    void Subscribe(string channel, Action<PluginMessage> handler);
    void Unsubscribe(string channel, Action<PluginMessage> handler);
    void Publish(string channel, ReadOnlyMemory<byte> payload, IOrionPlugin? sender = null);
}
```

Canal: `namespace:name` (ex.: `economy:balance-changed`).

### Pacotes soft API

`Economy.Api` (interfaces) ← `Economy.Plugin` registra; `Shop.Plugin` softdepend + `TryGet` / Messenger. Shared types de `IEconomy` só se o host allowlistar.

## 4. Sequência

1. `OnEnable` respeita softdepend.
2. Providers `Register` cedo; consumers `TryGet` tarde ou on-demand.
3. `Publish` após Enable; v1 callbacks na **mesma thread** do Publish.
4. `OnDisable` → unregister + unsubscribe.

## 5. Arquivos a tocar

Implementações `ServiceRegistry` / `PluginMessenger`; expor em `IPluginContext`; `/plugins` mostra `provides`.

## 6. Testes de aceitação

- Sem provider: `TryGet` false.
- Com provider + ordem softdepend: `TryGet` ok.
- Dois providers: maior `ServicePriority` vence.
- `Publish` entrega a todos os subscribers.
- Após disable, não cachear service eternamente.

## 7. Notas de migração

Nada equivalente hoje. Sample de pares fica para exemplos futuros.

## 8. Status

`implemented`

## Padrões “A ouve B”

| Padrão | Quando |
|--------|--------|
| Ambos no signal de **core** | Domínio já existe |
| B `Publish`, A Subscribe | Eventos frouxos sem DLL compartilhada |
| B `Register<IFoo>`, A `TryGet` | API imperativa rica |
| Hard `depend` | A não funciona sem B |

## O que não faremos

- IL weaving entre plugins.
- Carregar o assembly de B no ALC de A para ver tipos internos.
- Merge silencioso de services conflitantes.
