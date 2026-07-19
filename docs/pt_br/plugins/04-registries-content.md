# Fase 4 — Registries e conteúdo

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/04-registries-content.md`](../../en_us/plugins/04-registries-content.md)

## 1. Objetivo

Oferecer **registries de conteúdo explícitos** (estilo palettes Serenity/Aether) para plugins registrarem itens, blocos, comandos, generators e creative tabs por APIs estáveis — sem acessar tipos privados do Orion.

## 2. Não-objetivos

- Pipeline completo de geometria/RP Bedrock no core.
- Sync automático de todas as receitas vanilla.
- Dois plugins donos do mesmo identifier sem tooling de conflito (Fase 7).

## 3. Esboço de API pública

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

public interface ICreativeTabRegistry
{
    /// <summary>1 Construction, 3 Equipment, 4 Items. 2 Nature reservado ao core.</summary>
    void AddEntry(string pluginId, int category, string identifier);
}

public interface IWorldInitContext
{
    IOrionWorld World { get; }
    IContentRegistries Registries { get; }
}
```

(Demais registries: ver twin EN para records `ItemRegistration` / `BlockRegistration`.)

### Quando registrar

| Conteúdo | Fase preferida |
|----------|----------------|
| Fillers pré-freeze do catálogo | `Load` (hoje) → buffer até build do catálogo |
| Types ligados ao mundo | `OnWorldInitialize` |
| Comandos / services | `OnEnable` |
| Generators globais | `Load` ou `OnEnable` antes de criar o mundo |

### Ownership

- Primeiro `Register` bem-sucedido **dona** o identifier.
- Segundo ⇒ reject + log; `ConflictMode` na Fase 7.

## 4. Sequência

1. `Load`: enqueue no buffer.
2. Catálogo consome buffer + `orion/items.json`.
3. Mundo / pregen.
4. `OnWorldInitialize`: extensões de palette.
5. Join envia payloads já congelados (sem resend mid-session).

## 5. Arquivos a tocar

| Path | Mudança |
|------|---------|
| [`CuratedItemCatalog.cs`](../../../src/Protocol/Registry/CuratedItemCatalog.cs) | Backend de creative/item |
| [`ItemRegistry.cs`](../../../src/Orion/Item/ItemRegistry.cs) | Path público |
| [`BlockRegistry.cs`](../../../src/Orion/Block/BlockRegistry.cs) | Facade pública |
| Commands / generators | Registro de plugin |
| MinimalInventoryItems | `CreativeTabs.AddEntry` |

## 6. Testes de aceitação

- Cobble/sword/stick via registry = comportamento atual.
- Nature rejeitado na API de plugin.
- Identifier duplicado ⇒ warn/fail.
- Comando em `OnEnable` aparece quando aplicável.
- Mutação após freeze ⇒ `InvalidOperationException`.

## 7. Notas de migração

| Hoje | Alvo |
|------|------|
| `CuratedItemCatalog.RegisterCreativeTabEntries` | `ICreativeTabRegistry.AddEntry` |
| `RegisterBlock` privado | `IBlockRegistry.Register` |
| Traits só no assembly Orion | `RegisterFromAssembly` no plugin (ex.: `VanillaAttributes`) |

## 8. Status

`implemented`

## Paralelo Aether

Definir types no plugin → agregar → registrar no world init.

## Resource packs

Visuais client exigem RP separado; core não embute texturas do plugin.
