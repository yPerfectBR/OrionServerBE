# Fase 12 — Registries ricos e traits (final)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/12-sdk-registries-traits.md`](../../en_us/plugins/12-sdk-registries-traits.md)  
**Depende de:** [11 — Orion.Api](11-sdk-orion-api-surface.md), [04 — Registries](04-registries-content.md)

## 1. Goal

Substituir o registro fino (allowlist/hash) pela **superfície final rica**: states, components, tags, propriedades físicas e **trait registries** de bloco/item/entidade via `IContentRegistries`, sem chamar internals de `Orion.dll`.

## 2. Non-goals

- Autorar o catálogo vanilla Bedrock inteiro via plugins.
- Un-register após freeze.
- Hot-add de network IDs após o client start sem protocolo de sync documentado.

## 3. Freeze (contrato)

| Fase | Quando | Permitido |
|------|--------|-----------|
| Aberto | `Load` até init do catálogo | items, blocks, traits, creative |
| Items/Creative frozen | Após `NotifyCatalogLoaded` | sem novos items/creative |
| Blocks frozen | `FreezeBlocks` | sem novos blocks |
| Generators frozen | Após bootstrap do mundo | sem novos generators |

## 4. Public API sketch

```csharp
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

public interface IBlockTraitRegistry
{
    void RegisterFromAssembly(Assembly assembly, string pluginId);
    void Register(Type traitType, string pluginId);
}
// IItemTraitRegistry, IEntityTraitRegistry — mesmo shape
```

Bases `BlockTraitBase` / `ItemTraitBase` em `Orion.Api`. Convenção static `Identifier` / `Types` / `Tags` / `State` / `Component` preservada.

Exemplo `Load`: registrar `BlockRegistration` rico + `BlockTraits.RegisterFromAssembly`.

## 5. File touch list

Expandir DTOs; interfaces de trait registry; facades → `*TraitRegistry` interno; Vanilla\* só via `context.Registries`.

## 6. Acceptance tests

- Bloco custom com states + trait funciona pós-world.
- Register pós-freeze lança.
- Terceiro não referencia `BlockTraitRegistry` de Orion.dll.
- Nature (cat. 2) continua bloqueada para fillers.

## 7. Migration notes

- Construtor fino permanece via defaults; path suportado é o rico.
- `RegisterFromAssembly` estático em Orion vira internal.

## 8. Status

`spec`
