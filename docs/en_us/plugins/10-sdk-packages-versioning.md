# Phase 10 — SDK packages, versioning & McMaster sharing

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/10-sdk-packages-versioning.md`](../../pt_br/plugins/10-sdk-packages-versioning.md)  
**Depends on:** [09 — SDK overview](09-sdk-overview.md)

## 1. Goal

Specify the **final** project layout, NuGet identities, semver rules, `plugin.json` `api` validation, and the complete McMaster `SharedAssemblies` allowlist so compile-time and runtime type identity match for deep plugins.

## 2. Non-goals

- Publishing the Orion **implementation** assembly (`Orion.dll`) as a NuGet.
- Multi-targeting below `net10.0`.
- Private NuGet feeds as a hard requirement (GitHub Packages **or** nuget.org are both valid; CI must pack locally in either case).

## 3. Project layout (final)

```
src/
  PluginContracts/          # existing — Orion.PluginContracts
  Orion.Api/                # NEW — Orion.Api
  Orion.Gameplay.Api/       # NEW — Orion.Gameplay.Api
  Orion/                    # implementation — references Api projects; NOT published as SDK
  Protocol/                 # optional escape NuGet for authors
plugins/
  VanillaContainers/
  VanillaInventory/
  ...
```

### Project references (implementation)

```
Orion.csproj
  → PluginContracts
  → Orion.Api
  → Orion.Gameplay.Api
  → Protocol, World, …

Orion.Gameplay.Api.csproj
  → Orion.Api
  → PluginContracts

Orion.Api.csproj
  → PluginContracts
  → (minimal shared value types: prefer types defined in Orion.Api or already in contracts;
     Protocol value types used in facades are re-exported or wrapped — see §6)

Vanilla*.csproj (final)
  → PackageReference PluginContracts, Orion.Api, Orion.Gameplay.Api
  → PackageReference sibling Vanilla*.Api when needed
  → NO ProjectReference to Orion.csproj
```

## 4. NuGet package metadata

| PackageId | Project | Version source |
|-----------|---------|----------------|
| `Orion.PluginContracts` | `src/PluginContracts/PluginContracts.csproj` | `<Version>` = SDK train (e.g. `0.1.0`) |
| `Orion.Api` | `src/Orion.Api/Orion.Api.csproj` | Same train |
| `Orion.Gameplay.Api` | `src/Orion.Gameplay.Api/Orion.Gameplay.Api.csproj` | Same train |

All three packages in one release **share the same `Version`** (the SDK train). Breaking changes bump **major** (or `0.x` minor while pre-1.0).

### Plugin PackageReference pattern (final)

```xml
<ItemGroup>
  <PackageReference Include="Orion.PluginContracts" Version="0.1.0">
    <ExcludeAssets>runtime</ExcludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Orion.Api" Version="0.1.0">
    <ExcludeAssets>runtime</ExcludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Orion.Gameplay.Api" Version="0.1.0">
    <ExcludeAssets>runtime</ExcludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>
```

`ExcludeAssets=runtime` ensures the plugin does **not** ship a private copy of shared contracts; the host ALC wins.

When a plugin **must** use Protocol escape hatch ([15](15-sdk-protocol-escape.md)):

```xml
<PackageReference Include="Orion.Protocol" Version="0.1.0" />
<!-- runtime assets allowed — private copy in plugin folder is OK; not in SharedAssemblies -->
```

## 5. `plugin.json` `api` validation (final)

Existing field ([02](02-lifecycle-manifest.md)):

```json
"api": "0.1.0"
```

**Final rule:**

| Condition | Result |
|-----------|--------|
| Plugin `api` major > host SDK major | **Fatal** — refuse load |
| Plugin `api` major == host, minor > host minor | **Fatal** — refuse load |
| Plugin `api` major == host, minor ≤ host minor | Load (host is backward compatible within major) |
| Plugin `api` major < host major | **Fatal** unless host documents a compatibility window (default: fatal) |

Host SDK version = `Orion.Api` assembly informational version (same train as PluginContracts).

Implement in `PluginManifest` load path / `PluginHost.LoadConfigured` before McMaster load.

## 6. McMaster SharedAssemblies (complete final list)

In [`PluginHost.cs`](../../../src/Orion/Plugins/PluginHost.cs), share **entire assemblies** (not only a subset of types) for:

1. `Orion.PluginContracts`
2. `Orion.Api`
3. `Orion.Gameplay.Api`

Plus first-party `Vanilla*.Api` assemblies that the host allowlists when those plugins are loaded (same pattern as [05](05-services-messaging.md) Foo.Api).

**Do not share:**

- `Orion` (implementation)
- `Protocol` (unless a future decision promotes selected types into Orion.Api wrappers)
- Plugin implementation assemblies

Remove the current escape of sharing `typeof(Server)` for VanillaAttributes — after dogfood ([17](17-sdk-vanilla-dogfood.md)), Vanilla\* use facades only.

### PreferSharedTypes

Keep `PreferSharedTypes = true` on `PluginLoader` so shared assemblies unify.

## 7. Public API sketch — packing

```xml
<!-- Orion.Api.csproj -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <PackageId>Orion.Api</PackageId>
  <Version>0.1.0</Version>
  <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  <Authors>Orion</Authors>
  <Description>Stable gameplay facades for Orion Bedrock server plugins.</Description>
</PropertyGroup>
```

CI target:

```bash
dotnet pack src/PluginContracts/PluginContracts.csproj -c Release -o artifacts/nuget
dotnet pack src/Orion.Api/Orion.Api.csproj -c Release -o artifacts/nuget
dotnet pack src/Orion.Gameplay.Api/Orion.Gameplay.Api.csproj -c Release -o artifacts/nuget
```

Local authoring feed:

```bash
dotnet nuget add source ./artifacts/nuget -n OrionLocal
```

## 8. File touch list

| Path | Change |
|------|--------|
| `src/Orion.Api/Orion.Api.csproj` | Create |
| `src/Orion.Gameplay.Api/Orion.Gameplay.Api.csproj` | Create |
| `src/PluginContracts/PluginContracts.csproj` | Ensure packable metadata aligned |
| `src/Orion/Orion.csproj` | ProjectReference Api packages; drop Vanilla InternalsVisibleTo |
| `src/Orion/Plugins/PluginHost.cs` | SharedAssemblies = three SDK assemblies; api validation |
| `src/Orion/Plugins/PluginManifest.cs` | Compare api vs host |
| CI workflow | pack + optional publish |
| `Directory.Build.props` (optional) | Central `OrionSdkVersion` |

## 9. Acceptance tests

- `dotnet pack` produces three nupkgs with identical Version.
- External plugin with ExcludeAssets=runtime restores and compiles; published plugin folder contains **no** `Orion.Api.dll`.
- Host load shares `IPlayer` type identity: `ReferenceEquals(typeof(IPlayer).Assembly, pluginLoadedType.Assembly)` for facade types.
- Plugin with `"api": "9.0.0"` against host `0.1.0` fails boot with clear error.
- Plugin with `"api": "0.1.0"` against host `0.2.0` (same major) loads.

## 10. Migration notes

- Existing PluginContracts version `0.1.0` becomes the first SDK train.
- Gameplay interfaces move out of `Orion` into `Orion.Gameplay.Api` in the same train as first Orion.Api publish.
- Vanilla\* switch to PackageReference in [17](17-sdk-vanilla-dogfood.md) — not a separate temporary ProjectReference path.

## 11. Status

`spec`
