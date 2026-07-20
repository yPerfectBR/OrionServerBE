# Phase 16 — External plugin author guide (final)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/16-sdk-external-plugin-guide.md`](../../pt_br/plugins/16-sdk-external-plugin-guide.md)  
**Depends on:** [09](09-sdk-overview.md)–[15](15-sdk-protocol-escape.md)

## 1. Goal

Provide the **final** end-to-end authoring guide: project template, `plugin.json`, publish layout, and complete walkthroughs for the use-cases in [09](09-sdk-overview.md). Authors never clone the Orion monorepo.

## 2. Non-goals

- Teaching C# / .NET basics.
- Guaranteeing binary compatibility across major SDK bumps without recompile.

## 3. Project template (final)

### plugin.json

```json
{
  "id": "MyPlugin",
  "version": "1.0.0",
  "api": "0.1.0",
  "description": "Example deep plugin",
  "authors": ["You"],
  "main": "MyPlugin.MyPluginMain",
  "depend": [],
  "softdepend": ["VanillaInventory"],
  "loadbefore": [],
  "provides": []
}
```

### MyPlugin.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>MyPlugin</AssemblyName>
    <RootNamespace>MyPlugin</RootNamespace>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <OutputPath>$(MSBuildThisFileDirectory)bin\</OutputPath>
    <CopyLocalLockFileAssemblies>false</CopyLocalLockFileAssemblies>
  </PropertyGroup>
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
  <ItemGroup>
    <None Update="plugin.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
  <Target Name="CopyPluginBesideManifest" AfterTargets="Build">
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(MSBuildThisFileDirectory)" SkipUnchangedFiles="true" />
  </Target>
</Project>
```

### On-disk layout after build

```
plugins/MyPlugin/
  plugin.json
  MyPlugin.dll
  (private dependency DLLs if any — not Orion.Api / PluginContracts)
```

Enable in `server.json`: `"Plugins": { "Enabled": true, "Directory": "plugins" }`.

## 4. Walkthrough A — Soft shop (integration)

**Packages:** PluginContracts + `Economy.Api` (third-party).  
**Manifest:** `"softdepend": ["Economy"]`.

```csharp
public sealed class ShopPlugin : IOrionPlugin
{
    public string Id => "Shop";
    public Version Version { get; } = new(1, 0, 0);
    IEconomy? _economy;

    public void Load(IPluginLoadContext context) { }

    public void OnEnable(IPluginContext context)
    {
        context.Services.TryGet(out _economy);
        context.Registries.Commands.Register(new BuyCommand(_economy));
    }

    public void OnWorldInitialize(IWorldInitContext context) { }
    public void OnDisable(IPluginContext context) { }
}
```

## 5. Walkthrough B — Minigame kit (inventory deep)

**Packages:** PluginContracts + Orion.Api + Gameplay.Api.  
**Manifest:** `"softdepend": ["VanillaInventory"]` (or `depend` if required).

```csharp
public void OnEnable(IPluginContext context)
{
    context.Events.Subscribe<PlayerJoinSignal>(signal =>
    {
        if (!context.Services.TryGet(out IPlayerInventoryService? inv) || inv is null)
            return;
        inv.TryClear(signal.Player);
        inv.TryGive(signal.Player, Items.Create("minecraft:iron_sword", 1)!, out _);
        inv.TryGive(signal.Player, Items.Create("minecraft:bread", 16)!, out _);
        signal.Player.SendMessage("§aKit applied.");
    });
}
```

## 6. Walkthrough C — Custom ore block (content deep)

**Packages:** PluginContracts + Orion.Api (+ Gameplay.Api if giving drops via inventory).

```csharp
public void Load(IPluginLoadContext context)
{
    context.Registries.Blocks.Register(new BlockRegistration(
        Identifier: "myplugin:deep_ore",
        DefaultStateHash: /* from tooling or constant */,
        Solid: true,
        Hardness: 3f,
        Tags: ["stone"]));
    context.Registries.BlockTraits.RegisterFromAssembly(typeof(MyPluginMain).Assembly, Id);
}

public void OnEnable(IPluginContext context)
{
    context.Events.Subscribe<PlayerBreakBlockSignal>(signal =>
    {
        var block = signal.Player.Dimension?.GetBlock(
            signal.BlockPosition.X, signal.BlockPosition.Y, signal.BlockPosition.Z);
        if (block?.Type.Identifier != "myplugin:deep_ore")
            return;
        if (context.Services.TryGet(out IPlayerInventoryService? inv) && inv is not null)
            inv.TryGive(signal.Player, Items.Create("minecraft:diamond", 1)!, out _);
    });
}

public sealed class DeepOreTrait : BlockTraitBase
{
    public static string Identifier => "myplugin:deep_ore_trait";
    public static readonly string[] Types = ["myplugin:deep_ore"];
    public DeepOreTrait(IBlock block) : base(block) { }
}
```

## 7. Walkthrough D — Cancel high builds (events)

```csharp
context.Events.Subscribe<PlayerPlaceBlockSignal>(s =>
{
    if (s.BlockPosition.Y >= 320)
    {
        s.Cancel();
        s.Player.SendMessage("§cToo high.");
    }
}, EventPriority.High);
```

## 8. Walkthrough E — Packet observe (escape)

```csharp
context.Packets.OnReceive(/* packet id */, ctx =>
{
    // metrics only — do not steal VanillaInventory ownership
}, EventPriority.Monitor);
```

Use Protocol PackageReference only if constructing packets with no Orion.Api helper ([15](15-sdk-protocol-escape.md)).

## 9. Checklist for authors

- [ ] `api` matches installed server SDK train  
- [ ] Folder name == `id` == assembly name  
- [ ] No `Orion.Api.dll` copied next to plugin (ExcludeAssets=runtime)  
- [ ] Soft features use `TryGet`, not hard `depend`, unless required  
- [ ] No `InternalsVisibleTo` hacks  
- [ ] Tested against a released server build, not a private monorepo path  

## 10. File touch list (SDK ship)

| Path | Change |
|------|--------|
| `templates/OrionPlugin/` | `dotnet new` template matching §3 |
| Sample `plugins/examples/DeepOreSample/` | Optional in-repo sample using PackageReference to packed nupkgs |

## 11. Acceptance tests

- Fresh machine: nuget restore → build → copy to `plugins/` → server loads plugin; kit walkthrough runs with VanillaInventory.
- Deep ore registers and trait loads without Orion source tree present.

## 12. Status

`spec`
