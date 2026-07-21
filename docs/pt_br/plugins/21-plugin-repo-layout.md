# Fase 21 — Layout de repositório (plugins first-party)

**Status:** `implemented`  
**Versão em inglês:** [`../../en_us/plugins/21-plugin-repo-layout.md`](../../en_us/plugins/21-plugin-repo-layout.md)

## 1. Objetivo

Layout padrão em `plugins/` com ids `orion:*`, pasta `src/` e convenção de assembly.

## 2. Árvore alvo (exemplo `orion:inventory`)

```
plugins/orion:inventory/
  plugin.json
  README.md
  OrionInventory.csproj
  src/
    OrionInventoryPlugin.cs
    Handlers/
    Traits/
  bin/
  orion.inventory.dll
```

## 3. Regras de nome

| Item | Regra |
|------|--------|
| Pasta | Igual ao `id` (`orion:inventory`) |
| `AssemblyName` | `orion.inventory` (`:` → `.`) |
| `RootNamespace` | PascalCase (`OrionInventory`) |
| `main` | `{RootNamespace}.{ClassePlugin}` |
| `IOrionPlugin.Id` | Igual ao manifest `id` |

## 4. `.csproj`

```xml
<AssemblyName>orion.inventory</AssemblyName>
<RootNamespace>OrionInventory</RootNamespace>
```

Target pós-build copia a DLL ao lado de `plugin.json`.

## 5. MSBuild e `:` no caminho

`plugins/Directory.Build.props` redireciona `obj/`/`bin/` para `plugins/.msbuild/{ProjectName}/`. **Não remover** ao adicionar plugins `orion:*`.

## 6. Mapa de migração

| Antigo | Novo id | Assembly |
|--------|---------|----------|
| VanillaContainers | `orion:containers` | `orion.containers.dll` |
| VanillaInventory | `orion:inventory` | `orion.inventory.dll` |
| VanillaContainerBlocks | `orion:block-containers` | `orion.block-containers.dll` |
| VanillaAttributes | `orion:attributes` | `orion.attributes.dll` |
| VanillaBuilding | `orion:building` | `orion.building.dll` |
| VanillaMining | `orion:mining` | `orion.mining.dll` |
| MinimalItems | `orion:minimal-items` | `orion.minimal-items.dll` |

## 7. Checklist de migração

1. Renomear pasta → `orion:*`.
2. Mover `.cs` → `src/`.
3. Atualizar `.csproj` e `plugin.json` v2.
4. Ajustar `ProjectReference` entre plugins.
5. Build → DLL ao lado do manifest.
6. Atualizar [first-run](../first-run.md).

## Relacionados

- [19 — Manifest v2](19-manifest-v2.md)
- [20 — Guia do desenvolvedor](20-plugin-developer-guide.md)
