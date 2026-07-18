# Fase 1 — Loader e contratos (McMaster)

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/01-loader-contracts-mcmaster.md`](../../en_us/plugins/01-loader-contracts-mcmaster.md)

## 1. Objetivo

Carregar **todos** os plugins C# **exclusivamente** com **McMaster.NETCore.Plugins** (ALC + deps privadas + `sharedTypes` de contratos). O `PluginHost` do Orion só orquestra discovery/`plugin.json`/lifecycle — **nunca** carrega assemblies por conta própria.

Introduzir o projeto fino **`Orion.PluginContracts`** que plugins referenciam em vez do monolito Orion.

## 2. Não-objetivos

- Qualquer loader custom: `Assembly.LoadFrom`, `AssemblyLoadContext` caseiro, scan recursivo de `*.dll` sem McMaster, ou dual-path “McMaster + fallback”.
- Sandbox de segurança contra plugins maliciosos (confiança: só o que o operador instalou).
- Hot-reload na Fase 1 (`isUnloadable` pode existir, sem uso).
- Carregar plugins sob Native AOT do Orion.
- Auto-ativar plugins; `Plugins.Enabled` permanece default `false`.

## 3. Esboço de API pública

### Assembly de contratos (`Orion.PluginContracts`)

```csharp
namespace Orion.PluginContracts;

public interface IOrionPlugin
{
    string Id { get; }
    Version Version { get; }

    /// <summary>Após load do assembly, antes do Server existir. Só registro pré-catálogo.</summary>
    void Load(IPluginLoadContext context);

    /// <summary>Após ServerHost.Bootstrap. Eventos, services, comandos.</summary>
    void OnEnable(IPluginContext context);

    /// <summary>Mundo pronto para registro de conteúdo (palettes / generators).</summary>
    void OnWorldInitialize(IWorldInitContext context);

    void OnDisable(IPluginContext context);
}

public interface IPluginLoadContext
{
    IPluginManifest Manifest { get; }
    // Só logger — sem Server
}

public interface IPluginContext
{
    IPluginManifest Manifest { get; }
    IOrionServer Server { get; }
    IServiceRegistry Services { get; }
    IPluginMessenger Messenger { get; }
    IEventBus Events { get; }
}

public interface IPluginManifest
{
    string Id { get; }
    Version Version { get; }
    Version ApiVersion { get; }
    IReadOnlyList<string> Depend { get; }
    IReadOnlyList<string> SoftDepend { get; }
    IReadOnlyList<string> LoadBefore { get; }
    IReadOnlyList<string> Provides { get; }
}
```

Facades (`IOrionServer`, `IEventBus`, …) ficam nos contratos; implementações no projeto Orion.

### Host (Orion)

```csharp
namespace Orion.Plugins;

public static class PluginHost
{
    public static IReadOnlyList<IOrionPlugin> Loaded { get; }
    public static void LoadConfigured(OrionConfig config);
    public static void EnableAll(IOrionServer server);
    public static void InitializeWorld(IWorldInitContext world);
    public static void DisableAll();
}
```

### Uso McMaster (host)

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
    ],
    isUnloadable: false);

Assembly assembly = loader.LoadDefaultAssembly();
```

### Layout em disco

```
plugins/
  MinimalInventoryItems/
    plugin.json
    MinimalInventoryItems.dll
    (deps privadas do dotnet publish)
```

Convenção: nome da pasta == assembly == `id` do manifest.

### Config

```json
"Plugins": {
  "Enabled": false,
  "Directory": "plugins"
}
```

## 4. Sequência de boot / runtime

1. `OrionInfo.Load` + logger.
2. Se `Plugins.Enabled`: enumerar `plugins/*/plugin.json`; topo-sort (Fase 2); McMaster + `Load`.
3. `ItemRegistry.EnsureLoaded` / catálogo.
4. `ServerHost.Bootstrap`.
5. `EnableAll` → `OnEnable`.
6. Mundo pronto → `OnWorldInitialize`.

## 5. Arquivos a tocar

| Path | Mudança |
|------|---------|
| Novo `src/PluginContracts/PluginContracts.csproj` | TFM `net10.0` |
| [`src/Orion/Orion.csproj`](../../../src/Orion/Orion.csproj) | PackageReference McMaster; ProjectReference contracts; AOT só em perfil sem plugins |
| [`src/Orion/Plugins/PluginHost.cs`](../../../src/Orion/Plugins/PluginHost.cs) | Trocar `LoadFrom` |
| [`src/Orion/Plugins/IOrionPlugin.cs`](../../../src/Orion/Plugins/IOrionPlugin.cs) | Mover/obsoletar → contracts |
| [`plugins/MinimalInventoryItems/`](../../../plugins/MinimalInventoryItems/) | Só contracts; publish na pasta; `plugin.json` |
| [`src/Server/Program.cs`](../../../src/Server/Program.cs) | Load antes do catálogo; Enable após bootstrap |

## 6. Testes de aceitação

- `Enabled: false`: sem McMaster; Nature-only + aviso.
- Sample publicado + `Enabled: true`: load McMaster; `/plugins` lista id.
- Plugin com NuGet privado sem contaminar o ALC do host.
- `IsAssignableFrom(IOrionPlugin)` funciona via `sharedTypes`.
- Build do plugin **não** copia Orion.dll ao lado (`Private=false`).

## 7. Notas de migração

| Removido | Alvo |
|----------|------|
| Stub `Assembly.LoadFrom` + scan de DLL | **Apagado** — só McMaster `PluginLoader` |
| Plugin referencia Orion completo | `Orion.PluginContracts` (+ Protocol temporário para creative até Fase 4) |
| `void Load()` sem manifest | Load / OnEnable / OnWorldInitialize / OnDisable + `plugin.json` |

## 8. Status

`spec`

## Regras de shared types

1. Só tipos de **`Orion.PluginContracts`** (e facades allowlisted) são shared.
2. Plugins não publicam cópia privada de contracts (`ExcludeAssets=runtime`).
3. APIs entre dois plugins de terceiros usam pacote **`Foo.Api`** — ver Fase 5.

## Nota AOT

`PublishAot` Release é **incompatível** com plugins dinâmicos. Perfis com plugins = **managed** (`PublishAot=false`).
