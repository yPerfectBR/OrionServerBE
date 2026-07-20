# Fase 19 — Manifest v2

**Status:** `implemented`  
**Versão em inglês:** [`../../en_us/plugins/19-manifest-v2.md`](../../en_us/plugins/19-manifest-v2.md)  
**Substitui:** [02 — Ciclo de vida e manifest](02-lifecycle-manifest.md) nos campos de dependência (`depend` / `softdepend` como objetos; `loadbefore` removido).

## 1. Objetivo

Definir **`plugin.json` v2**: ids com namespace (`prefixo:produto`), dependências como objetos com intervalos SemVer, ordenação determinística, validação fatal e resolução de assembly a partir do `id`.

## 2. Fora de escopo

- Compatibilidade com v1 (`depend: ["string"]`, `loadbefore`) — breaking change intencional.
- Campo `nome` de exibição separado do `id`.
- Marketplace / hot-reload em runtime.

## 3. Schema

```json
{
  "id": "orion:inventory",
  "version": "1.0.0",
  "api": "0.1.0",
  "description": "Runtime de inventário do jogador",
  "authors": ["Orion"],
  "main": "OrionInventory.OrionInventoryPlugin",
  "depend": [
    { "id": "orion:containers", "versions": ["1.0.0", "2.0.0"] }
  ],
  "softdepend": [
    { "id": "orion:attributes", "load": "before", "versions": ["1.0.0", "99.0.0"] }
  ],
  "provides": ["orion:inventory"]
}
```

| Campo | Obrigatório | Significado |
|-------|-------------|-------------|
| `id` | sim | Id único; **deve ser igual ao nome da pasta** |
| `version` | sim | Versão SemVer do plugin |
| `api` | sim | API mínima do host (ver [10](10-sdk-packages-versioning.md)) |
| `main` | sim | Tipo que implementa `IOrionPlugin` |
| `depend` | não | Dependências duras — alvo ausente ⇒ fatal; alvo carrega **antes** |
| `softdepend` | não | Ordenação opcional quando o alvo existe |
| `provides` | não | Nomes de capability para descoberta (não são ids de plugin) |

### 3.1 `id` e pasta

- Formato: `prefixo:produto`, segmentos `[a-z_]+`, cada um ≤ 18 caracteres.
- Pasta em `plugins/` **deve ser igual** ao `id`.
- Regex da pasta: `^[a-z0-9:-]{1,25}$`.

### 3.2 `depend` (dura)

```json
{ "id": "orion:containers", "versions": ["1.0.0", "2.0.0"] }
```

- `versions`: intervalo SemVer inclusivo `[min, max]`.
- **Sem campo `load`** — dependência dura sempre implica ordem fixa.
- Alvo **obrigatório** no boot.

### 3.3 `softdepend` (opcional)

```json
{ "id": "orion:attributes", "load": "after", "versions": ["1.0.0", "99.0.0"] }
```

| `load` | Aresta | Significado |
|--------|--------|-------------|
| `"after"` (padrão) | `alvo → este` | Este plugin depois do alvo, se ambos existirem |
| `"before"` | `este → alvo` | Este plugin antes do alvo, se ambos existirem |

- Alvo ausente: aresta ignorada.
- `versions` opcional; se presente, versão instalada deve estar no intervalo.

### 3.4 Removido: `loadbefore`

Use `softdepend` com `"load": "before"`.

## 4. Grafo de ordem

| Aresta | Origem |
|--------|--------|
| `dep → plugin` | `depend` |
| `soft → plugin` | `softdepend` com `load: "after"` |
| `plugin → soft` | `softdepend` com `load: "before"` |

Ordenação topológica com desempate alfabético. Ciclo ⇒ fatal.

## 5. Restrições de versão

1. `versions` com exatamente dois elementos SemVer.
2. Versão instalada do alvo deve satisfazer `min ≤ versão ≤ max`.
3. **Conflito entre plugins:** interseção vazia de intervalos sobre o mesmo `id` ⇒ `VERSION_CONSTRAINT_CONFLICT`.

## 6. Códigos de erro fatal

| Código | Quando |
|--------|--------|
| `MANIFEST_REGEX` | `id`, pasta ou `versions` inválidos |
| `DEPEND_MISSING` | `depend` aponta para plugin ausente |
| `VERSION_OUT_OF_RANGE` | Versão fora do intervalo da aresta |
| `VERSION_CONSTRAINT_CONFLICT` | Intervalos disjuntos no mesmo alvo |
| `ORDER_CYCLE` | Ciclo no grafo |
| `API_MISMATCH` | Campo `api` incompatível com o host |

## 7. Resolução de assembly

```csharp
string dllName = id.Replace(':', '.') + ".dll";
```

`AssemblyName` no `.csproj` deve seguir o mesmo padrão. Ver [21 — Layout de repositório](21-plugin-repo-layout.md).

## 8. Logging

Descoberta, ordem de carga, McMaster, conflitos de serviço/pacote e validação de manifest usam **`LogCategory.Plugins`**. Configure em `config/server.json` → `Logging.LogLevel.Plugins`. Ver [20 — Guia do desenvolvedor](20-plugin-developer-guide.md).

## 9. Sequência de boot (trecho manifest)

1. Descobrir `plugins/*/plugin.json`.
2. Validar pasta = `id`.
3. Validar objetos de dependência.
4. Verificar `depend` duras.
5. Validar intervalos e interseção global.
6. Ordenação topológica.
7. Resolver `AssemblyPath`.
8. McMaster → `Load` → … (ver [02](02-lifecycle-manifest.md)).

## 10. Critérios de aceite

- [ ] `depend` como `string[]` não suportado.
- [ ] `orion:inventory` resolve `orion.inventory.dll`.
- [ ] Conflitos de versão e ciclos impedem o boot com mensagem codificada.
- [ ] Categoria `Plugins` nos logs do host.

## Relacionados

- [02 — Ciclo de vida](02-lifecycle-manifest.md)
- [20 — Guia do desenvolvedor](20-plugin-developer-guide.md)
- [21 — Layout](21-plugin-repo-layout.md)
