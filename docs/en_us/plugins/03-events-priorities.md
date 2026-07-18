# Phase 3 — Events & priorities

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/03-events-priorities.md`](../../pt_br/plugins/03-events-priorities.md)

## 1. Goal

Expose Orion’s existing signal bus to plugins through **contracts**, with **handler priority**, consistent **cancellation**, and documented **thread affinity** so plugins subscribe in `OnEnable` (after `Server` exists) rather than in pre-server `Load`.

## 2. Non-goals

- Reflection-based `onPlayerJoin` method naming (Serenity style) — Orion uses explicit typed subscribe.
- Cross-process event distribution.
- Guaranteeing two plugins’ mutations compose when both ignore cancel/priority rules.

## 3. Public API sketch

```csharp
namespace Orion.PluginContracts.Events;

public enum EventPriority
{
    Lowest = 0,
    Low = 1,
    Normal = 2,
    High = 3,
    Highest = 4,
    Monitor = 5  // must not mutate / cancel; observe only
}

public interface IEventBus
{
    void Subscribe<TSignal>(Action<TSignal> handler, EventPriority priority = EventPriority.Normal)
        where TSignal : ISignal;

    void Unsubscribe<TSignal>(Action<TSignal> handler) where TSignal : ISignal;

    // Optional convenience
    IDisposable SubscribeDisposable<TSignal>(Action<TSignal> handler, EventPriority priority = EventPriority.Normal)
        where TSignal : ISignal;
}

// Existing core concept — move shared pieces to contracts as needed
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

Plugin usage:

```csharp
public void OnEnable(IPluginContext ctx)
{
    ctx.Events.Subscribe<PlayerChatSignal>(OnChat, EventPriority.High);
}

void OnChat(PlayerChatSignal signal)
{
    if (signal.Message.Contains("bad", StringComparison.OrdinalIgnoreCase))
        signal.Cancel();
}
```

### Priority semantics

1. Handlers run **Highest → Lowest**, then **Monitor**.
2. If a cancellable signal is cancelled, later mutating priorities still run **unless** the bus supports `ignoreCancelled` (v1: continue calling all; handlers must check `Cancelled`). Spec v1.1 may add `Subscribe(..., ignoreCancelled: true)`.
3. **Monitor** handlers that call `Cancel()` or mutate state ⇒ host logs a warning (debug) and ignores cancel from Monitor.

### Thread affinity

Reuse [`SignalAffinity`](../../../src/Orion/Scheduling/SignalAffinity.cs):

| Events | Affinity |
|--------|----------|
| `ServerStart`, `PlayerJoin` | Global / main-ish path |
| Player/entity area events | Area thread when area threading enabled |

Plugins must not assume thread-local statics without affinity docs. Long work ⇒ schedule via `IOrionServer` job APIs (future) or fire-and-forget carefully.

## 4. Boot / runtime sequence

1. Core constructs `Server` with internal handler lists (today).
2. `OnEnable`: plugins call `ctx.Events.Subscribe`.
3. Core `Emit` sites unchanged; bus dispatches by priority.
4. `OnDisable`: host auto-unsubscribes handlers registered through `IPluginContext` (preferred) or plugin calls `Unsubscribe`.

## 5. File touch list

| Path | Change |
|------|--------|
| [`src/Orion/Server.cs`](../../../src/Orion/Server.cs) | Priority-ordered lists; `Off`/unsubscribe; wrap as `IEventBus` |
| [`src/Orion/Events/`](../../../src/Orion/Events/) | Ensure cancellable consistency; share `ISignal` in contracts |
| [`src/Orion/Scheduling/SignalAffinity.cs`](../../../src/Orion/Scheduling/SignalAffinity.cs) | Document + keep |
| Emit call sites (Login, Text, InventoryTransaction, …) | No API change; verify cancel honored |
| Contracts | `IEventBus`, `EventPriority`, `ICancellable` |

## 6. Acceptance tests

- Two handlers on `PlayerChatSignal`: Higher priority runs first; cancel prevents message broadcast when core checks `Cancelled`.
- Subscribe in `Load` without server ⇒ API unavailable / throws clear exception.
- Unsubscribe / disable removes handler (no leak after `OnDisable`).
- Monitor handler cannot cancel (warning + no effect).
- Area-affinity event still executes on area thread when enabled (existing scheduling tests extended).

## 7. Migration notes from current stub

| Today | Target |
|-------|--------|
| `Server.On<T>(ServerEvent, Action<T>)` | `IEventBus.Subscribe<T>` with priority |
| No subscribers in repo | Plugins + optional core modules subscribe |
| `Load()` before Server | Event subscribe only in `OnEnable` |
| Cancel flags inconsistent | Normalize `ICancellable` on all cancellable signals |

## 8. Status

`implemented`

## Initial event surface (already in core enum)

Document and keep stable:

- `ServerStart`
- `PlayerJoin`, `PlayerSpawn`, `PlayerLeave`, `PlayerChat`
- `PlayerPlaceBlock`, `PlayerBreakBlock`
- `EntityHurt`, `EntitySpawn`, `EntityDie`

Grow the enum carefully; prefer new signals over overloading packets (Phase 6) once a domain concept stabilizes.

## Inter-plugin “events”

Plugins do **not** emit into another plugin’s private C# event blindly. Cross-plugin notifications use:

1. Core domain signals (both subscribe), or  
2. Phase 5 **Messenger** channels / **Services** callbacks.

That keeps load graphs soft and avoids hard assembly references.
