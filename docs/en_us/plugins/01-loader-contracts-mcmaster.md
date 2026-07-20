# Phase 1 — Loader & contracts (McMaster)

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/01-loader-contracts-mcmaster.md`](../../pt_br/plugins/01-loader-contracts-mcmaster.md)

## 1. Goal

Load **all** C# plugins **exclusively** through **McMaster.NETCore.Plugins** (ALC + private deps + contract `sharedTypes`). Orion’s `PluginHost` only orchestrates discovery/`plugin.json`/lifecycle — it **never** loads assemblies itself.

Introduce a thin **`Orion.PluginContracts`** project that plugins reference instead of the Orion monolith.

## 2. Non-goals

- Any custom loader: `Assembly.LoadFrom`, hand-rolled `AssemblyLoadContext`, recursive `*.dll` scan without McMaster, or dual-path “McMaster + fallback”.
- Sandboxing / security isolation against malicious plugins (trust model: operator-installed only).
- Hot-reload in Phase 1 (unload hooks land later; McMaster `isUnloadable` may be wired but unused).
- Loading plugins under Native AOT publish of Orion.
- Auto-enabling plugins; `Plugins.Enabled` remains default `false`.

## 3. Public API sketch

### Contracts assembly (`Orion.PluginContracts`)

```csharp
namespace Orion.PluginContracts;

public interface IOrionPlugin
{
    string Id { get; }
    Version Version { get; }

    /// <summary>Called after assembly load, before Server exists. Register content that must precede catalog init only.</summary>
    void Load(IPluginLoadContext context);

    /// <summary>Called after ServerHost bootstrap. Subscribe events, register services, commands.</summary>
    void OnEnable(IPluginContext context);

    /// <summary>Called when a world is ready for content registration (palettes / generators).</summary>
    void OnWorldInitialize(IWorldInitContext context);

    void OnDisable(IPluginContext context);
}

public interface IPluginLoadContext
{
    IPluginManifest Manifest { get; }
    // Logger facade only — no Server
}

public interface IPluginContext
{
    IPluginManifest Manifest { get; }
    IOrionServer Server { get; }          // facade over Orion.Server
    IServiceRegistry Services { get; }
    IPluginMessenger Messenger { get; }
    IEventBus Events { get; }
}

public interface IPluginManifest
{
    string Id { get; }
    Version Version { get; }
    IReadOnlyList<string> Depend { get; }
    IReadOnlyList<string> SoftDepend { get; }
    IReadOnlyList<string> LoadBefore { get; }
    IReadOnlyList<string> Provides { get; }
}
```

Facades (`IOrionServer`, `IEventBus`, …) live in contracts; implementations wrap core types in the Orion project.

### Host (Orion)

```csharp
namespace Orion.Plugins;

public static class PluginHost
{
    public static IReadOnlyList<IOrionPlugin> Loaded { get; }
    public static void LoadConfigured(OrionConfig config); // McMaster discovery
    public static void EnableAll(IOrionServer server);
    public static void InitializeWorld(IWorldInitContext world);
    public static void DisableAll();
}
```

### McMaster usage (host)

```csharp
var loader = PluginLoader.CreateFromAssemblyFile(
    assemblyFile: pluginDllPath,
    sharedTypes:
    [
        typeof(IOrionPlugin),
        typeof(IPluginContext),
        typeof(IPluginLoadContext),
        typeof(IPluginManifest),
        typeof(IEventBus),
        typeof(IServiceRegistry),
        typeof(IPluginMessenger)
        // + future contract types
    ],
    isUnloadable: false); // true only when unload is implemented

Assembly assembly = loader.LoadDefaultAssembly();
```

### On-disk layout

```
plugins/
  MinimalInventoryItems/
    plugin.json
    MinimalInventoryItems.dll
    (plugin-private dependency DLLs from dotnet publish)
```

Convention: folder name == assembly name == manifest `id` (case-sensitive recommendation: PascalCase id).

### Config (unchanged shape)

```json
"Plugins": {
  "Enabled": false,
  "Directory": "plugins"
}
```

## 4. Boot / runtime sequence

1. `OrionInfo.Load` + logger init.
2. If `Plugins.Enabled`:
   - Enumerate `plugins/*/plugin.json` (not recursive `*.dll` under `obj/`).
   - Topological sort by `depend` / `softdepend` / `loadbefore` (Phase 2).
   - For each plugin: `PluginLoader.CreateFromAssemblyFile`, find `IOrionPlugin`, `Load(IPluginLoadContext)`.
3. `ItemRegistry.EnsureLoaded` / catalog init (may consume pre-init registrations).
4. `ServerHost.Bootstrap`.
5. `PluginHost.EnableAll(server)` → `OnEnable`.
6. After world ready: `OnWorldInitialize`.

## 5. File touch list

| Path | Change |
|------|--------|
| New `src/PluginContracts/PluginContracts.csproj` | Contracts TFM `net10.0` |
| [`src/Orion/Orion.csproj`](../../../src/Orion/Orion.csproj) | PackageReference McMaster; ProjectReference contracts; **do not** enable AOT when plugin host is linked — or gate AOT to a non-plugin publish profile |
| [`src/Orion/Plugins/PluginHost.cs`](../../../src/Orion/Plugins/PluginHost.cs) | Replace `LoadFrom` |
| [`src/Orion/Plugins/IOrionPlugin.cs`](../../../src/Orion/Plugins/IOrionPlugin.cs) | Move/obsolete → contracts |
| [`plugins/MinimalInventoryItems/`](../../../plugins/MinimalInventoryItems/) | Reference contracts only; `dotnet publish` into own folder; add `plugin.json` |
| [`src/Server/Program.cs`](../../../src/Server/Program.cs) | Keep load-before-catalog; add Enable after bootstrap |

## 6. Acceptance tests

- With `Enabled: false`, no McMaster load; creative Nature-only + warning still works.
- With sample published under `plugins/MinimalInventoryItems/` and `Enabled: true`, plugin loads via McMaster; `/plugins` lists id.
- Plugin can depend on a private NuGet (e.g. a JSON lib) without that assembly being shared into the host ALC incorrectly.
- `typeof(IOrionPlugin).IsAssignableFrom(pluginType)` succeeds across ALC because of `sharedTypes`.
- Build of plugin does **not** copy Orion.dll next to the plugin (`Private=false` / publish settings documented).

## 7. Migration notes

| Removed | Target |
|---------|--------|
| Stub `Assembly.LoadFrom` + DLL scan | **Deleted** — McMaster `PluginLoader` only |
| Plugin references full Orion | `Orion.PluginContracts` (+ Protocol temporarily for creative until Phase 4) |
| Parameterless `Load()` without manifest | Load / OnEnable / OnWorldInitialize / OnDisable + `plugin.json` |

## 8. Status

`spec`

## Shared types rules

1. Only types from **`Orion.PluginContracts`** (and explicitly allowlisted host facades) are shared.
2. Plugins must **not** ship a private copy of contracts in their publish output (reference with `ExcludeAssets=runtime` or equivalent so the host’s contracts win).
3. Integration APIs between two third-party plugins use a **third** package (`Foo.Api`) — see Phase 5; do not put third-party APIs into Orion contracts.

## AOT note

`PublishAot` on Orion Release is **incompatible** with dynamic plugin loading. Spec: plugin-capable publish profiles remain **managed** (`PublishAot=false`). Document in server README / first-run when implementing.
