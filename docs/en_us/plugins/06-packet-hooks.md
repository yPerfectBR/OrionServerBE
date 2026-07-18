# Phase 6 — Packet hooks

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/06-packet-hooks.md`](../../pt_br/plugins/06-packet-hooks.md)

## 1. Goal

Add **low-level packet receive/send hooks** so a plugin can implement gameplay the core does not yet expose (example: projectile physics driven by movement/input packets), following Endstone `PacketReceiveEvent` / `PacketSendEvent` and PocketMine `DataPacketReceiveEvent`.

This is an **escape hatch**, not the default API for ordinary features.

## 2. Non-goals

- Stable guarantee of raw payload layout across every Bedrock protocol bump without versioning.
- Letting plugins replace the entire network stack.
- Zero-cost abstraction — hooks sit on a hot path and must be opt-in per PacketId.

## 3. Public API sketch

```csharp
namespace Orion.PluginContracts.Network;

public interface IPacketPipeline
{
    /// <summary>Subscribe to inbound packets after framing/decode of PacketId, before core handler switch.</summary>
    void OnReceive(PacketHook hook);

    /// <summary>Subscribe to outbound packets before write to connection.</summary>
    void OnSend(PacketHook hook);

    /// <summary>Claim exclusive handling for a PacketId. Conflicts → Phase 7 rules.</summary>
    bool TryOwnHandler(int packetId, IOrionPlugin owner, PacketHandlerDelegate handler);
}

public delegate void PacketHandlerDelegate(PacketReceiveContext context);

public sealed class PacketHook
{
    public required IOrionPlugin Plugin { get; init; }
    public int? PacketIdFilter { get; init; } // null = all (discouraged)
    public EventPriority Priority { get; init; } = EventPriority.Normal;
    public required Action<PacketReceiveContext> OnReceive { get; init; } // or send variant
}

public sealed class PacketReceiveContext : ICancellable
{
    public required IPlayerConnection Connection { get; init; }
    public required int PacketId { get; init; }
    public required ReadOnlyMemory<byte> Payload { get; init; } // body without frame header
    public bool Cancelled { get; private set; }
    public void Cancel() => Cancelled = true;

    /// <summary>If set by an owning handler, core switch skips default handler.</summary>
    public bool Handled { get; set; }
}

public sealed class PacketSendContext : ICancellable
{
    public required IPlayerConnection Connection { get; init; }
    public required int PacketId { get; init; }
    public required ReadOnlyMemory<byte> Payload { get; init; }
    public bool Cancelled { get; private set; }
    public void Cancel() => Cancelled = true;
    /// <summary>Optional replacement body (same PacketId).</summary>
    public byte[]? ReplacementPayload { get; set; }
}
```

### Prefer high-level events when they exist

| Need | Prefer |
|------|--------|
| Chat moderation | `PlayerChatSignal` |
| Block break | `PlayerBreakBlockSignal` |
| Custom projectile sync | Packet hooks + future `Projectile*` signals |
| Entirely new client packet | Packet hooks until core adds codec + event |

### Example: projectile plugin (sketch)

1. `TryOwnHandler` or `OnReceive` filter for relevant input / entity packets.
2. Maintain plugin-side simulation state.
3. `OnSend` / server send APIs to push entity motion packets.
4. When core later adds `ProjectileLaunchSignal`, migrate off raw hooks.

## 4. Boot / runtime sequence

Inbound (per packet):

1. RakNet → unframe → read `PacketId` + payload.
2. Emit receive hooks (priority order); if `Cancelled`, drop.
3. If `Handled` by owner, skip core `switch`.
4. Else existing [`NetworkHandler`](../../../src/Orion/Network/) / handlers.

Outbound:

1. Core or plugin requests send.
2. Emit send hooks; cancel drops; replacement mutates body.
3. Write to connection.

## 5. File touch list

| Path | Change |
|------|--------|
| [`src/Orion/Network/`](../../../src/Orion/Network/) PacketIngress / NetworkHandler | Insert hook points |
| Protocol `PacketId` enum | Expose via contracts or mirror ints |
| New `PacketPipeline` in Orion | Implements `IPacketPipeline` |
| [`IPluginContext`](01-loader-contracts-mcmaster.md) | Expose `Packets` |
| Perf: skip hook invocation when no subscribers for that PacketId | Dictionary of lists |

## 6. Acceptance tests

- Hook with filter cancels a packet ⇒ core handler not called (instrumented counter).
- `TryOwnHandler` second owner rejected; first remains.
- Subscribe-all (`PacketIdFilter: null`) logs warning once per plugin.
- Benchmark/smoke: no subscribers ⇒ near-zero overhead (early out).
- Protocol docs note: payload is version-specific to `Raknet.Protocol` in config.

## 7. Migration notes from current stub

Today: closed `switch` in worker — no extension. Phase 6 opens the pipe without removing core handlers.

## 8. Status

`implemented`

Hook payloads are version-specific to the configured protocol (`Raknet.Protocol` / host codec).

## Safety & performance rules

1. **Always filter** by `PacketId` in production plugins.
2. Do not allocate per-packet on Monitor paths; prefer pooled buffers.
3. Do not block area/session threads on I/O inside hooks.
4. Deserialization: prefer core packet types when available (`TryGetPacket<T>()` future helper); raw bytes otherwise.
5. Malicious/heavy plugins can stall the server — operator trust model applies.

## References

- Endstone: `PacketReceiveEvent`, `PacketSendEvent`
- PocketMine: `DataPacketReceiveEvent` (cancellable)
- Paper: high-level events first; packet-level is rare on Java
