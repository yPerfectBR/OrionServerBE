# Phase 4 — Registries & content

**Status:** `implemented`  
**Language twin:** [`../../pt_br/plugins/04-registries-content.md`](../../pt_br/plugins/04-registries-content.md)

## 1. Goal

Provide **explicit content registries** (Serenity/Aether-style palettes) so plugins register items, blocks, commands, generators, and creative-tab entries through stable APIs — instead of poking private Orion types.

## 2. Non-goals

- Full custom Bedrock block geometry / RP authoring pipeline in core (plugins + resource packs remain separate).
- Automatic sync of every vanilla recipe.
- Allowing two plugins to own the same namespaced identifier without conflict tooling (see Phase 7).

## 3. Public API sketch

```csharp
namespace Orion.PluginContracts.Registry;

public interface IContentRegistries
{
    IItemRegistry Items { get; }
    IBlockRegistry Blocks { get; }
    ICommandRegistry Commands { get; }
    ICreativeTabRegistry CreativeTabs { get; }
    IGeneratorRegistry Generators { get; }
}

public interface IItemRegistry
{
    /// <summary>Register or overlay an allowlisted/giveable item from the vanilla palette or a custom definition.</summary>
    void Register(ItemRegistration registration);
    bool IsRegistered(string identifier);
}

public interface IBlockRegistry
{
    void Register(BlockRegistration registration);
}

public interface ICreativeTabRegistry
{
    /// <summary>Category: 1 Construction, 3 Equipment, 4 Items. Category 2 Nature reserved for core curated world blocks.</summary>
    void AddEntry(string pluginId, int category, string identifier);
}

public interface IGeneratorRegistry
{
    void Register(string name, Type generatorType); // or factory delegate
}

public interface ICommandRegistry
{
    void Register(IPluginCommand command);
}

public sealed record ItemRegistration(
    string Identifier,
    bool Creative = true,
    int? CreativeCategory = null,
    // future: components, max stack, block link
    ...);

public sealed record BlockRegistration(
    string Identifier,
    int DefaultStateHash,
    bool Solid = true,
    ...);
```

`IWorldInitContext`:

```csharp
public interface IWorldInitContext
{
    IOrionWorld World { get; }
    IContentRegistries Registries { get; }
}
```

### When to register

| Content | Preferred phase |
|---------|-----------------|
| Creative tab fillers that must exist before catalog freeze | `Load` (today) → migrate to registry that buffers until catalog build |
| Block/item types bound to a world | `OnWorldInitialize` |
| Commands / services | `OnEnable` |
| Generators (global map) | `Load` or `OnEnable` before world create |

### Ownership

- First successful `Register` for identifier `minecraft:foo` / `aether:holystone` **owns** it.
- Second owner ⇒ reject + log (Phase 7); optional config `Plugins.ConflictMode: fail | warn`.

## 4. Boot / runtime sequence

1. `Load`: plugins may enqueue creative/item registrations into a **pending buffer**.
2. Catalog / `ItemRegistry.EnsureLoaded` consumes buffer + core `orion/items.json`.
3. World created / pregen.
4. `OnWorldInitialize`: block/item palette extensions for that world; recipes later.
5. Join sends ItemRegistry + CreativeContent payloads already built (no mid-session resend — Bedrock crash risk).

## 5. File touch list

| Path | Change |
|------|--------|
| [`src/Protocol/Registry/CuratedItemCatalog.cs`](../../../src/Protocol/Registry/CuratedItemCatalog.cs) | Back `ICreativeTabRegistry` / item allowlist |
| [`src/Orion/Item/ItemRegistry.cs`](../../../src/Orion/Item/ItemRegistry.cs) | Public registration path |
| [`src/Orion/Block/BlockRegistry.cs`](../../../src/Orion/Block/BlockRegistry.cs) | Make registration public via facade |
| [`src/Orion/Commands/`](../../../src/Orion/Commands/) | Plugin command registration |
| World generation factory | Generator registry |
| Sample MinimalInventoryItems | Use `CreativeTabs.AddEntry` instead of static catalog call |

## 6. Acceptance tests

- Plugin registers cobble/sword/stick via registry; creative packet groups match today’s behavior.
- Nature category rejected from plugin creative API.
- Duplicate identifier from second plugin ⇒ warn/fail per config; first wins.
- Command registered in `OnEnable` appears in `AvailableCommands` soft enums when applicable.
- Mid-session registry mutation does not resend ItemRegistry (documented; test asserts API throws `InvalidOperationException` after freeze).

## 7. Migration notes from current stub

| Today | Target |
|-------|--------|
| `CuratedItemCatalog.RegisterCreativeTabEntries` | `ICreativeTabRegistry.AddEntry` |
| Private `BlockRegistry.RegisterBlock` | `IBlockRegistry.Register` |
| Traits only from Orion assembly | `RegisterFromAssembly(pluginAssembly)` or explicit trait API later |

## 8. Status

`implemented`

## Aether parallel

the-aether pattern to mirror:

```text
OnWorldInitialize:
  for type in BlockTypes → blockPalette.registerType
  for type in ItemTypes → itemPalette.registerType
  for recipe in Recipes → registerRecipe
```

Orion names differ, sequence matches: **define in plugin modules → aggregate → register in world init**.

## Resource packs

Custom blocks/items with client visuals require an RP. Core does not embed plugin textures; operators enable packs via existing resources config. Document pairing `plugin id` ↔ RP UUID in plugin README convention.
