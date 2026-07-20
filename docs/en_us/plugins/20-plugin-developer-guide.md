# Phase 20 — Plugin developer guide

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/20-plugin-developer-guide.md`](../../pt_br/plugins/20-plugin-developer-guide.md)  
**Depends on:** [19 — Manifest v2](19-manifest-v2.md), [09 — SDK overview](09-sdk-overview.md)

## 1. Audience

Authors of **third-party or first-party** Orion plugins after manifest v2 and the neutral gameplay APIs ship.

## 2. Environment

| Requirement | Value |
|-------------|--------|
| SDK | .NET 10 |
| NuGet (target) | `Orion.PluginContracts`, `Orion.Api`, `Orion.Gameplay.Api` |
| PackageReference | `ExcludeAssets="runtime"` — host supplies implementations via McMaster |
| Monorepo (today) | `ProjectReference` to SDK projects + `PluginContracts`; **no** `Orion.csproj` long-term |

## 3. Project layout

See [21 — Plugin repo layout](21-plugin-repo-layout.md). Summary:

```
plugins/orion:my-plugin/
  plugin.json
  README.md
  OrionMyPlugin.csproj
  src/
    OrionMyPluginPlugin.cs
  orion.my-plugin.dll   # post-build copy beside manifest
```

- Folder name **must equal** manifest `id` (`prefix:product`).
- `AssemblyName` = `id` with `:` → `.` (e.g. `orion.inventory`).

## 4. Manifest v2 quick reference

- **`depend`**: hard — target must exist; loads before you; `versions: [min, max]` inclusive.
- **`softdepend`**: optional — `load: "after"` (default) or `"before"`; ignored if target missing.
- **`provides`**: capability strings for docs/diagnostics — **not** plugin ids.
- **`api`**: minimum host PluginContracts version ([10](10-sdk-packages-versioning.md)).

Example soft conflict:

```json
"softdepend": [
  { "id": "orion:inventory", "load": "after", "versions": ["1.0.0", "99.0.0"] }
]
```

If plugin A needs `orion:foo` **before** itself when both exist:

```json
"softdepend": [
  { "id": "orion:foo", "load": "before", "versions": ["1.0.0", "99.0.0"] }
]
```

## 5. Services & replacement

1. **Capability owner** — one plugin registers the highest-priority `Register<T>` for a service type; advertise via `provides`.
2. **Packet owner** — `TryOwnHandler` is exclusive (first wins). Do **not** steal `ItemStackRequest` from `orion:inventory`.
3. **Replace inventory UX** — cancel `PlayerOpenInventorySignal` or provide `IPlayerInventoryService` without loading `orion:inventory`.
4. **Hard `depend`** — functional + fixed load order; no `load` field.

See [14 — Gameplay services](14-sdk-gameplay-services.md) replacement policy.

## 6. Packet pipeline

| Mode | API | Use |
|------|-----|-----|
| Observe | `OnReceive` / `OnSend` (Monitor) | Metrics, logging |
| Own | `TryOwnHandler` | Exclusive handler — one owner per PacketId |

Extend inventory via `IPlayerInventoryService` / events, not by owning ISR after `orion:inventory`.

## 7. Logging (`LogCategory.Plugins`)

Plugin discovery, load order, McMaster load, registry/packet/service conflicts, and manifest validation log under **`Plugins`**.

`config/server.json`:

```json
"Plugins": {
  "Debug": true,
  "Info": true,
  "Warn": true,
  "Error": true,
  "Chat": false
}
```

Enable `Debug` when diagnosing load-order or `TryOwnHandler` conflicts.

## 8. Troubleshooting (boot failures)

| Symptom / code | Likely cause | Fix |
|-----------------|--------------|-----|
| `MANIFEST_REGEX` | Bad `id`, folder ≠ `id`, invalid `versions` | Use `orion:product`; match folder name |
| `DEPEND_MISSING` | Hard `depend` target not in `plugins/` | Add plugin or change to `softdepend` |
| `VERSION_OUT_OF_RANGE` | Installed plugin outside edge range | Bump dependency version or widen range |
| `VERSION_CONSTRAINT_CONFLICT` | Two plugins require disjoint ranges on same id | Align ranges or remove one plugin |
| `ORDER_CYCLE` | Circular before/after + depend | Fix `softdepend.load` / hard deps |
| `API_MISMATCH` | `api` in manifest < host | Update plugin or host |
| Assembly not found | DLL name ≠ `id` with `:` → `.` | Set `AssemblyName` in csproj |
| McMaster load fail | Missing shared contract types | Reference same PluginContracts version as host |

## 9. Best practices

- Prefer **`softdepend`** for optional integrations; use **`depend`** only when the plugin cannot load without the target.
- Use SemVer ranges that reflect real compatibility — avoid `99.0.0` max in production manifests unless intentional.
- Do not cache `IServiceRegistry` results across `OnDisable` — services unregister on shutdown.
- Unsubscribe events in `OnDisable` (or use disposable subscriptions).
- Publish integration types in **`YourPlugin.Api`** NuGet for other third parties ([05](05-services-messaging.md)).

## 10. Walkthroughs

Hands-on samples: [16 — External plugin guide](16-sdk-external-plugin-guide.md).

## Related

- [19 — Manifest v2](19-manifest-v2.md)
- [21 — Repo layout](21-plugin-repo-layout.md)
- [First run](../first-run.md)
