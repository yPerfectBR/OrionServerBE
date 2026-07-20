# Phase 19 — Manifest v2

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/19-manifest-v2.md`](../../pt_br/plugins/19-manifest-v2.md)  
**Supersedes:** [02 — Lifecycle & manifest](02-lifecycle-manifest.md) for dependency fields (`depend` / `softdepend` object form; `loadbefore` removed).

## 1. Goal

Define **`plugin.json` v2**: namespaced plugin ids (`prefix:product`), object-shaped dependencies with SemVer ranges, deterministic load ordering, fatal validation, and assembly resolution from id.

## 2. Non-goals

- Backward compatibility with v1 `depend: ["string"]` or `loadbefore` (breaking change).
- Display `name` field separate from `id` (may be added later).
- Runtime plugin marketplace / hot-reload.

## 3. Schema

```json
{
  "id": "orion:inventory",
  "version": "1.0.0",
  "api": "0.1.0",
  "description": "Player inventory runtime",
  "authors": ["Orion"],
  "main": "OrionInventory.OrionInventoryPlugin",
  "depend": [
    { "id": "orion:containers", "versions": ["1.0.0", "2.0.0"] }
  ],
  "softdepend": [
    { "id": "orion:attributes", "load": "before", "versions": ["1.0.0", "99.0.0"] }
  ],
  "provides": ["orion:inventory"]
}
```

| Field | Required | Meaning |
|-------|----------|---------|
| `id` | yes | Unique plugin id; **must equal folder name** |
| `version` | yes | SemVer plugin version |
| `api` | yes | Minimum Orion PluginContracts API (host validates — see [10](10-sdk-packages-versioning.md)) |
| `main` | yes | Fully qualified type implementing `IOrionPlugin` |
| `depend` | no | Hard dependencies — missing target ⇒ fatal; target loads **before** this plugin |
| `softdepend` | no | Optional ordering when target exists |
| `provides` | no | Capability names for discovery (not plugin ids) |

### 3.1 `id` and folder

- Format: `prefix:product` where both segments match `[a-z_]+`, each ≤ 18 characters.
- Folder name under `plugins/` **must equal** `id`.
- Folder regex: `^[a-z0-9:-]{1,25}$`.

### 3.2 `depend` (hard)

Array of objects:

```json
{ "id": "orion:containers", "versions": ["1.0.0", "2.0.0"] }
```

- `id`: target plugin id (same rules as manifest `id`).
- `versions`: inclusive SemVer range `[min, max]` (two-element array).
- **No `load` field** — hard dependency always means “target loads first”.
- Target **must** be present at boot; otherwise fatal.

### 3.3 `softdepend` (optional)

```json
{ "id": "orion:attributes", "load": "after", "versions": ["1.0.0", "99.0.0"] }
```

| `load` | Edge | Meaning |
|--------|------|---------|
| `"after"` (default) | `target → this` | Load this plugin after target when both exist |
| `"before"` | `this → target` | Load this plugin before target when both exist |

- If target is absent, edge is ignored (plugin still loads).
- `versions` optional; when present, installed target version must fall in range or boot is fatal.

### 3.4 Removed: `loadbefore`

Use `softdepend` with `"load": "before"` instead.

## 4. Load-order graph

| Edge | Source |
|------|--------|
| `dep → plugin` | `depend` (hard) |
| `soft → plugin` | `softdepend` with `load: "after"` |
| `plugin → soft` | `softdepend` with `load: "before"` |

Topological sort with alphabetical tie-break among zero-indegree nodes. Cycle ⇒ fatal.

## 5. Version constraints

For each edge with `versions: [min, max]`:

1. Parse `min` and `max` as `System.Version`.
2. Installed target version must satisfy `min ≤ version ≤ max` (inclusive).
3. **Cross-plugin conflict:** collect all ranges imposed on the same target id. If their intersection is empty, no installed version can satisfy all plugins ⇒ fatal even before per-edge checks in some cases.

Example: plugin A requires `orion:containers` in `[1.0, 2.0]`; plugin C requires `orion:containers` in `[3.0, 4.0]`. With `orion:containers` at `2.5`, ranges are incompatible ⇒ `VERSION_CONSTRAINT_CONFLICT`.

## 6. Fatal error codes

| Code | When |
|------|------|
| `MANIFEST_REGEX` | Invalid `id`, folder name, or `versions` format |
| `DEPEND_MISSING` | Hard `depend` target not discovered |
| `VERSION_OUT_OF_RANGE` | Installed version outside edge `versions` |
| `VERSION_CONSTRAINT_CONFLICT` | Disjoint ranges on same target from multiple plugins |
| `ORDER_CYCLE` | Cycle in dependency graph |
| `API_MISMATCH` | Plugin `api` incompatible with host (see [10](10-sdk-packages-versioning.md)) |

Messages should include plugin id, target id, and the offending range.

## 7. Assembly resolution

```csharp
// id "orion:inventory" → "orion.inventory.dll" beside plugin.json
string dllName = id.Replace(':', '.') + ".dll";
```

`AssemblyName` in the plugin `.csproj` should match (`orion.inventory`). See [21 — Plugin repo layout](21-plugin-repo-layout.md).

Fallback: single non-framework DLL in plugin folder (development convenience).

## 8. Logging

Plugin discovery, load order, McMaster load, service/packet conflicts, and manifest validation use **`LogCategory.Plugins`**. Configure in `config/server.json` under `Logging.LogLevel.Plugins`. See [20 — Plugin developer guide](20-plugin-developer-guide.md).

## 9. Boot sequence (manifest slice)

1. Discover `plugins/*/plugin.json`.
2. Validate folder name matches `id` regex.
3. Validate all dependency object shapes and version arrays.
4. Check hard dependencies exist.
5. Validate version ranges per edge and cross-constraint intersection.
6. Build graph and topological sort (or fatal).
7. Resolve `AssemblyPath` per plugin.
8. McMaster load in order → `Load` → … (see [02](02-lifecycle-manifest.md) lifecycle).

## 10. Acceptance criteria

- [ ] v1 string-array `depend` rejected or unsupported.
- [ ] `orion:inventory` resolves `orion.inventory.dll`.
- [ ] Cross-constraint and cycle failures stop boot with coded messages.
- [ ] `Plugins` log category routes host plugin diagnostics.

## Related

- [02 — Lifecycle](02-lifecycle-manifest.md) (lifecycle methods; deps superseded by this doc)
- [20 — Developer guide](20-plugin-developer-guide.md)
- [21 — Repo layout](21-plugin-repo-layout.md)
