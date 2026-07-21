# Phase 21 — First-party plugin repo layout

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/21-plugin-repo-layout.md`](../../pt_br/plugins/21-plugin-repo-layout.md)

## 1. Goal

Standard directory layout for plugins under `plugins/`, including manifest v2 ids (`orion:*`), `src/` source folder, and assembly naming.

## 2. Target tree (example `orion:inventory`)

```
plugins/orion:inventory/
  plugin.json
  README.md
  OrionInventory.csproj
  src/
    OrionInventoryPlugin.cs
    Handlers/
    Traits/
  bin/                    # build output (gitignored optional)
  orion.inventory.dll     # CopyPluginBesideManifest target
```

## 3. Naming rules

| Item | Rule |
|------|------|
| Folder | Equals manifest `id` (e.g. `orion:inventory`) |
| `AssemblyName` | `id` with `:` → `.` → `orion.inventory` |
| `RootNamespace` | PascalCase product (`OrionInventory`) |
| `main` in manifest | `{RootNamespace}.{PluginClass}` |
| `IOrionPlugin.Id` | Same string as manifest `id` |

## 4. `.csproj` essentials

```xml
<PropertyGroup>
  <RootNamespace>OrionInventory</RootNamespace>
  <AssemblyName>orion.inventory</AssemblyName>
  <OutputPath>$(MSBuildThisFileDirectory)bin\</OutputPath>
</PropertyGroup>
```

Post-build target copies `$(TargetPath)` beside `plugin.json` (see existing first-party projects).

## 5. MSBuild + colon in folder paths

Folder names contain `:` (required by spec). MSBuild on Linux mishandles `obj/` under those paths. The repo provides `plugins/Directory.Build.props` redirecting `obj/` and `bin/` to `plugins/.msbuild/{ProjectName}/`.

**Do not remove** that file when adding new `orion:*` plugins.

## 6. First-party id map (current)

| Legacy folder | New id | Assembly |
|---------------|--------|----------|
| VanillaContainers | `orion:containers` | `orion.containers.dll` |
| VanillaInventory | `orion:inventory` | `orion.inventory.dll` |
| VanillaContainerBlocks | `orion:block-containers` | `orion.block-containers.dll` |
| VanillaAttributes | `orion:attributes` | `orion.attributes.dll` |
| VanillaBuilding | `orion:building` | `orion.building.dll` |
| VanillaMining | `orion:mining` | `orion.mining.dll` |
| MinimalItems | `orion:minimal-items` | `orion.minimal-items.dll` |

## 7. Migration checklist

1. Rename folder → `orion:*`.
2. Move `*.cs` → `src/` (keep `Handlers/`, `Traits/`, etc. under `src/`).
3. Rename `.csproj`; set `AssemblyName` / `RootNamespace`.
4. Convert `plugin.json` to v2 objects ([19](19-manifest-v2.md)).
5. Update sibling `ProjectReference` paths.
6. `dotnet build` → verify DLL beside manifest.
7. Update [first-run](../first-run.md) build commands.

## Related

- [19 — Manifest v2](19-manifest-v2.md)
- [20 — Developer guide](20-plugin-developer-guide.md)
