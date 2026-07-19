# Phase 5 — Services & messaging

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/05-services-messaging.md`](../../pt_br/plugins/05-services-messaging.md)

## 1. Goal

Allow plugins to **integrate without hard load dependencies**: discover optional APIs at runtime (Services registry) and exchange fire-and-forget or request-style messages on namespaced channels (Messenger). Plugin A must run even if plugin B is absent.

## 2. Non-goals

- RPC framework with schemas/protobuf mandatory in v1 (bytes + typed helpers are enough).
- Guaranteeing delivery order across area threads without documenting affinity.
- Putting third-party service interfaces into `Orion.PluginContracts` (those live in `Foo.Api` packages).

## 3. Public API sketch

### Services registry (Bukkit ServicesManager analogue)

```csharp
namespace Orion.PluginContracts.Services;

public enum ServicePriority
{
    Lowest, Low, Normal, High, Highest
}

public interface IServiceRegistry
{
    void Register<TService>(TService instance, IOrionPlugin owner, ServicePriority priority = ServicePriority.Normal)
        where TService : class;

    void UnregisterAll(IOrionPlugin owner);

    bool TryGet<TService>(out TService? service) where TService : class;

    /// <summary>Highest priority registration wins for TryGet.</summary>
    TService GetRequired<TService>() where TService : class;
}
```

Provider plugin:

```csharp
// In EconomyPlugin (package Economy.Api defines IEconomy)
ctx.Services.Register<IEconomy>(new EconomyService(), this, ServicePriority.Normal);
```

Consumer (soft):

```csharp
public void OnEnable(IPluginContext ctx)
{
    if (ctx.Services.TryGet<IEconomy>(out var economy))
        _economy = economy;
}
```

Manifest: `"softdepend": ["Economy"]` only to order enable earlier when present — **not** required for TryGet.

### Messenger (namespaced bus)

```csharp
namespace Orion.PluginContracts.Messaging;

public interface IPluginMessenger
{
    void Subscribe(string channel, Action<PluginMessage> handler);
    void Unsubscribe(string channel, Action<PluginMessage> handler);
    void Publish(string channel, ReadOnlyMemory<byte> payload, IOrionPlugin? sender = null);
}

public sealed class PluginMessage
{
    public required string Channel { get; init; }  // "economy:balance-changed"
    public required ReadOnlyMemory<byte> Payload { get; init; }
    public string? SenderPluginId { get; init; }
}
```

Channel rules: `^[a-z0-9_.-]+:[a-z0-9_./-]+$` (namespace:name), similar spirit to Paper plugin channels.

Typed helper (optional conveniences in contracts or shared lib):

```csharp
Messenger.PublishJson("chat:format", new FormatRequest(...));
```

### Soft API packages

```
Economy.Api (net10.0)          ← interfaces only
Economy.Plugin                 ← implements, registers service
Shop.Plugin                    ← softdepend Economy; references Economy.Api with ExcludeAssets=runtime
                                ← sharedTypes may include typeof(IEconomy) IF host is configured to share it
```

**Sharing third-party contracts:** either

1. Host allowlists known API assemblies in McMaster `sharedTypes`, or  
2. Consumer uses reflection/`Type.GetType` + Messenger only (no shared interface).

Spec v1 recommends (1) for first-party companion APIs and Messenger for loose coupling.

## 4. Boot / runtime sequence

1. `OnEnable` order respects softdepend (Phase 2).
2. Providers `Register` services early in `OnEnable`.
3. Consumers `TryGet` late in `OnEnable` or on first use.
4. `Publish` may occur anytime after Enable; subscribers on other plugins receive callbacks on the publishing thread unless Messenger marshals to global (document: **same thread as Publish** in v1).
5. `OnDisable` → `UnregisterAll(owner)` + unsubscribe messenger handlers.

## 5. File touch list

| Path | Change |
|------|--------|
| New `Orion.Plugins.Services.ServiceRegistry` | Implementation |
| New `Orion.Plugins.Messaging.PluginMessenger` | Implementation |
| Contracts | Interfaces above |
| [`IPluginContext`](01-loader-contracts-mcmaster.md) | Expose `Services` + `Messenger` |
| Diagnostics `/plugins` | Show `provides` + registered service types |

## 6. Acceptance tests

- Consumer enables without provider: `TryGet` false; no throw.
- Provider + consumer: `TryGet` succeeds when softdepend order loads provider first.
- Two providers of `IEconomy`: highest `ServicePriority` returned by `TryGet`.
- `Publish` delivers to all subscribers of that channel.
- Disable provider: service unregistered; consumer’s stored reference may be stale — document “don’t cache across disable” or use `TryGet` each time.

## 7. Migration notes from current stub

Nothing equivalent today. Creative plugins do not need messaging. Introduce with a tiny sample pair in docs only (or later `plugins/examples/`).

## 8. Status

`implemented`

## “Plugin A listens to plugin B” patterns

| Pattern | Use when |
|---------|----------|
| Both subscribe to **core** `PlayerChatSignal` | Domain already exists |
| B `Publish("b:something")`, A subscribes | Loose events without shared DLL |
| B registers `IFoo`, A `TryGet<IFoo>` | Rich imperative API |
| Hard `depend` | A cannot function without B |

## What we will not do

- Automatic IL weaving to rewrite calls between plugins.
- Loading B’s assembly into A’s ALC to “see” internal types.
- Silent merge of conflicting service registrations — priority + diagnostics only.
