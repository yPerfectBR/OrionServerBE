# Fase 10 — Pacotes SDK, versionamento e sharing McMaster

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/10-sdk-packages-versioning.md`](../../en_us/plugins/10-sdk-packages-versioning.md)  
**Depende de:** [09 — Visão SDK](09-sdk-overview.md)

## 1. Goal

Especificar o layout **final** de projetos, identidades NuGet, semver dos pacotes SDK e a allowlist completa de `SharedAssemblies` do McMaster para identidade de tipos compile-time/runtime em plugins deep. **Não** há campo `api` no `plugin.json` nem versionamento de API de host.

## 2. Non-goals

- Publicar o assembly de **implementação** (`Orion.dll`) como NuGet.
- Multi-targeting abaixo de `net10.0`.
- Feed NuGet privado como requisito duro (GitHub Packages **ou** nuget.org; CI sempre gera pack local).

## 3. Layout de projetos (final)

```
src/
  PluginContracts/          # existente — Orion.PluginContracts
  Orion.Api/                # NOVO — Orion.Api
  Orion.Gameplay.Api/       # NOVO — Orion.Gameplay.Api
  Orion/                    # implementação — referencia Api; NÃO publicado como SDK
  Protocol/                 # NuGet opcional de escape
plugins/
  VanillaContainers/
  VanillaInventory/
  ...
```

### Project references (implementação)

```
Orion.csproj → PluginContracts, Orion.Api, Orion.Gameplay.Api, Protocol, World, …
Orion.Gameplay.Api.csproj → Orion.Api, PluginContracts
Orion.Api.csproj → PluginContracts

Vanilla*.csproj (final)
  → PackageReference PluginContracts, Orion.Api, Orion.Gameplay.Api
  → SEM ProjectReference a Orion.csproj
```

## 4. Metadados NuGet

| PackageId | Projeto | Versão |
|-----------|---------|--------|
| `Orion.PluginContracts` | `src/PluginContracts/` | Train SDK (ex. `0.1.0`) |
| `Orion.Api` | `src/Orion.Api/` | Mesmo train |
| `Orion.Gameplay.Api` | `src/Orion.Gameplay.Api/` | Mesmo train |

Os três pacotes de um release compartilham a **mesma** `Version`.

### Padrão PackageReference do plugin (final)

```xml
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
```

Escape Protocol ([15](15-sdk-protocol-escape.md)): PackageReference **com** runtime assets (cópia privada OK; não entra em SharedAssemblies).

## 5. `plugin.json` e versão de host

O manifest **não** declara versão de API do host. Compatibilidade de SDK fica implícita no train NuGet referenciado em compile-time; o boot **não** valida um campo `api`.

## 6. SharedAssemblies McMaster (lista final)

Compartilhar assemblies inteiros:

1. `Orion.PluginContracts`
2. `Orion.Api`
3. `Orion.Gameplay.Api`
4. `Vanilla*.Api` allowlistados first-party

**Não compartilhar:** `Orion` (implementação), `Protocol` (por padrão), DLLs de implementação de plugins.

Remover o share atual de `typeof(Server)` após dogfood ([17](17-sdk-vanilla-dogfood.md)).

## 7. Pack

```bash
dotnet pack src/PluginContracts/PluginContracts.csproj -c Release -o artifacts/nuget
dotnet pack src/Orion.Api/Orion.Api.csproj -c Release -o artifacts/nuget
dotnet pack src/Orion.Gameplay.Api/Orion.Gameplay.Api.csproj -c Release -o artifacts/nuget
```

## 8. File touch list

| Path | Mudança |
|------|---------|
| `src/Orion.Api/` | Criar |
| `src/Orion.Gameplay.Api/` | Criar |
| `PluginHost.cs` | SharedAssemblies |
| `Orion.csproj` | Refs Api; remover IVT Vanilla |
| CI | pack (+ publish opcional) |

## 9. Acceptance tests

- Três nupkgs com mesma Version.
- Plugin com ExcludeAssets=runtime **não** copia `Orion.Api.dll` para a pasta do plugin.
- Identidade de `IPlayer` unificada host/plugin.

## 10. Migration notes

- Train inicial `0.1.0`.
- Interfaces de gameplay saem de `Orion` no mesmo train do primeiro publish de Orion.Api.
- Vanilla\* migram em [17](17-sdk-vanilla-dogfood.md).

## 11. Status

`spec` — **auditoria jul/2026:** `Orion.Api` / `Orion.Gameplay.Api` **não existem** em `src/`; sem `dotnet pack` SDK; SharedAssemblies ainda não lista pacotes Api. Plugins em `Plugins-Orion/` ainda usam `ProjectReference` → `Orion.csproj`.
