# Phase 12 — Rich registries & traits (final)

**Status:** `spec`  
**Language twin:** [`../../pt_br/plugins/12-sdk-registries-traits.md`](../../pt_br/plugins/12-sdk-registries-traits.md)  
**Depends on:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [04 — Registries](04-registries-content.md)

## 1. Goal

Replace thin allowlist/hash-only block/item registration with the **final rich registry surface** plugins need for custom content: states, components, tags, physical properties, and first-class **trait registries** for blocks, items, and entities — all reachable from `IPluginLoadContext.Registries` / `Orion.Api` without calling `BlockTraitRegistry` internals on `Orion.dll`.

## 2. Non-goals

- Authoring the entire vanilla Bedrock catalog from plugins.
- Runtime un-registration after freeze.
- Hot-adding network IDs after client start without a documented palette sync protocol (freeze rules stay).

## 3. Freeze lifecycle (contract)

| Phase | When | Allowed |
|-------|------|---------|
| Open | `Load` through catalog init | Register items, blocks, traits, creative entries |
| Items/Creative frozen | After `ItemRegistry.EnsureLoaded` / `NotifyCatalogLoaded` | No new item/creative registrations |
| Blocks frozen | Same window as today (`FreezeBlocks`) | No new block registrations |
| Generators frozen | After world bootstrap | No new generators |

Violations throw with plugin id in the message (existing facade pattern).

## 4. Public API sketch

### 4.1 Expanded registrations

```csharp
namespace Orion.PluginContracts.Registry; // or Orion.Api.Registry — single owner: prefer Orion.Api for rich DTOs, contracts re-export

public sealed record BlockStateDefinition(string Name, IReadOnlyList<string> Values);

public sealed record BlockRegistration(
    string Identifier,
    int DefaultStateHash,
    bool Solid = true,
    bool Air = false,
    float Hardness = 0f,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyList<BlockStateDefinition>? States = null,
    IReadOnlyDictionary<string, string>? Components = null);

public sealed record ItemRegistration(
    string Identifier,
    bool Creative = true,
    int? CreativeCategory = null,
    int MaxStackSize = 64,
    IReadOnlyList<string>? Tags = null,
    IReadOnlyDictionary<string, string>? Components = null,
    string? BlockIdentifier = null);
```

`IBlockRegistry.Register` / `IItemRegistry.Register` accept these records. Host materializes `BlockType` / `ItemType` / permutations (including `BlockPermutation.Create` paths used today inside Orion).

### 4.2 Trait registries

```csharp
namespace Orion.Api.Registry;

public interface IBlockTraitRegistry
{
    /// <summary>Register trait types from plugin assembly (static Identifier/Types/Tags/State/Component).</summary>
    void RegisterFromAssembly(Assembly assembly, string pluginId);
    void Register(Type traitType, string pluginId);
}

public interface IItemTraitRegistry
{
    void RegisterFromAssembly(Assembly assembly, string pluginId);
    void Register(Type traitType, string pluginId);
}

public interface IEntityTraitRegistry
{
    void RegisterFromAssembly(Assembly assembly, string pluginId);
    void Register(Type traitType, string pluginId);
}
```

Expose on `IContentRegistries`:

```csharp
public interface IContentRegistries
{
    IItemRegistry Items { get; }
    IBlockRegistry Blocks { get; }
    IBlockTraitRegistry BlockTraits { get; }
    IItemTraitRegistry ItemTraits { get; }
    IEntityTraitRegistry EntityTraits { get; }
    ICommandRegistry Commands { get; }
    ICreativeTabRegistry CreativeTabs { get; }
    IGeneratorRegistry Generators { get; }
}
```

### 4.3 Trait base types in Orion.Api

Plugins subclass stable abstract bases **shipped in Orion.Api** (not Orion.dll):

```csharp
namespace Orion.Api.Blocks.Traits;

public abstract class BlockTraitBase
{
    protected BlockTraitBase(IBlock block) { Block = block; }
    protected IBlock Block { get; }
    public virtual void OnPlace(/* … */) { }
    public virtual void OnBreak(/* … */) { }
    public virtual void OnInteract(IPlayer player) { }
    // OnRead/OnWrite NBT via Orion.Api.Nbt facades or documented Protocol escape
}

namespace Orion.Api.Items.Traits;

public abstract class ItemTraitBase
{
    protected ItemTraitBase(IItemStack stack) { Stack = stack; }
    protected IItemStack Stack { get; }
    public virtual bool OnUseOnBlock(IPlayer player, BlockPos pos, int face) => false;
    public virtual bool OnUseOnAir(IPlayer player) => false;
}
```

Host adapters bridge these to existing [`BlockTrait`](../../../src/Orion/Block/Traits/BlockTrait.cs) / [`ItemTrait`](../../../src/Orion/Item/Traits/ItemTrait.cs) **or** those concrete classes become subclasses of the Api bases in the final ship (preferred dogfood).

Static discovery convention (unchanged semantics):

```csharp
public sealed class MyOreTrait : BlockTraitBase
{
    public static string Identifier => "myplugin:ore";
    public static readonly string[] Types = ["myplugin:deep_ore"];
    public MyOreTrait(IBlock block) : base(block) { }
}
```

### 4.4 Plugin Load example (final)

```csharp
public void Load(IPluginLoadContext context)
{
    context.Registries.Blocks.Register(new BlockRegistration(
        Identifier: "myplugin:deep_ore",
        DefaultStateHash: /* computed or provided */,
        Solid: true,
        Hardness: 3f,
        Tags: ["stone"],
        States: [new BlockStateDefinition("myplugin:quality", ["low", "high"])]));

    context.Registries.BlockTraits.RegisterFromAssembly(typeof(MyPlugin).Assembly, Id);
}
```

## 5. File touch list

| Path | Change |
|------|--------|
| Expand `BlockRegistration` / `ItemRegistration` | Rich fields |
| `Orion.Api.Registry` trait registry interfaces | New |
| `ContentRegistriesCore` / facades | Wire trait registries to existing `*TraitRegistry` |
| Move/adapt `BlockTrait` / `ItemTrait` bases | Orion.Api |
| VanillaContainerBlocks / VanillaAttributes | Register via `context.Registries.*Traits` only |

## 6. Acceptance tests

- Plugin registers custom block with states + trait in `Load`; after world ready, `IDimension.GetBlock` returns type with trait behavior on interact.
- Register after freeze throws.
- Third party never calls `BlockTraitRegistry` type from Orion.dll (type does not exist publicly / is internal).
- Creative Nature category (2) still rejected for plugin fillers (existing rule).

## 7. Migration notes

- Current thin `BlockRegistration(Identifier, DefaultStateHash, Solid, Air)` remains constructible via optional params / overloads for compatibility within the same major, but rich fields are the supported path.
- `RegisterFromAssembly` on Orion static registries becomes internal; plugins use `IContentRegistries`.

## 8. Status

`spec`
