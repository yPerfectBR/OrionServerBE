# First run (OrionServer)

Orion inicia com menu criativo **vazio** e **sem** blocos nativos de conteúdo. Nature, Construction, Equipment e Items ficam vazios até um plugin registrar (tipicamente `orion:minimal-items`).

## Gerador de mundo (default: void)

Mundos novos usam o generator **`void`** (chunks vazios). Spawn padrão: `[0, -57, 0]`. O core só tem o builtin **`void`** — não há fallback silencioso para void em outros ids.

Para terreno plano (bedrock / dirt / grass em Y −64…−60):

1. Ative plugins e faça deploy de `orion:minimal-items` + `orion:superflat` (superflat depende de minimal-items para os ids de bloco).
2. Configure o generator da dimensão overworld:

```json
"generator": "superflat"
```

Sem o plugin, `generator: "superflat"` (ou qualquer generator desconhecido / vazio) **recusa subir** com erro claro. O `identifier` da dimensão precisa ser conhecido (`overworld` / `nether` / `the_end`); vazio ou desconhecido também aborta o boot. Chunks LevelDB com blocos desconhecidos falham de forma dura (sem reescrever para air).

## Menu criativo vazio

O Bedrock pode mostrar o inventário criativo **inteiro** vazio quando Construction / Equipment / Items não têm itens. É limitação da UI do cliente.

## Correção recomendada (`orion:minimal-items`)

```bash
dotnet build plugins/orion:minimal-items/OrionMinimalItems.csproj
```

Ative em `config/server.json`:

```json
"Plugins": { "Enabled": true, "Directory": "plugins" }
```

O plugin registra os seis blocos Bedrock, Nature (grass/dirt/bedrock) e fillers nas outras abas.

## Vida / fome (attributes)

```bash
dotnet build plugins/orion:attributes/OrionAttributes.csproj
```

Com plugins ativos, registra traits **só via Orion.Api** (`EntityHealthTrait`, `PlayerHungerTrait`) e serviços (`provides: orion:attributes`, `orion:health`, `orion:hunger`). No join, reexibe as barras Health/Hunger e sincroniza atributos Bedrock (`minecraft:health`, `minecraft:player.hunger`, …). Consuma via `IAttributesApi` / `IEntityHealthService` / `IPlayerHungerService` / `IPlayerItemUseHandler`. Sem o plugin, HUD de vida/fome permanece oculto e as bridges do host são no-op. Prefira carregar com `orion:inventory` (softdepend) para decrementar stacks ao comer.

## Inventário, containers, building e mining

```bash
dotnet build plugins/orion:containers/OrionContainers.csproj
dotnet build plugins/orion:inventory/OrionInventory.csproj
dotnet build plugins/orion:block-containers/OrionBlockContainers.csproj
dotnet build plugins/orion:building/OrionBuilding.csproj
dotnet build plugins/orion:mining/OrionMining.csproj
```

Ordem de carga: manifest v2 ([plugins/19-manifest-v2.md](plugins/19-manifest-v2.md)).

- `orion:containers` — runtime de grades (`provides: orion:containers`)
- `orion:inventory` — inventário/ISR (`depend: orion:containers`, `provides: orion:inventory`)
- `orion:block-containers` — baú/barril
- `orion:building` / `orion:mining` — place e mineração (softdepend inventory)
- API: `IInventoryApi` / `IPlayerInventoryService`

Ver [plugins/20-plugin-developer-guide.md](plugins/20-plugin-developer-guide.md).

## Plugins externos (SDK)

[plugins/09-sdk-overview.md](plugins/09-sdk-overview.md) · [plugins/18-sdk-ai-implementation-checklist.md](plugins/18-sdk-ai-implementation-checklist.md)
