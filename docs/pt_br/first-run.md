# First run (OrionServer)

Orion inicia com menu criativo **mínimo**: só blocos Nature de `src/Protocol/Data/orion/items.json`. Construction, Equipment e Items ficam vazios até um plugin registrar fillers.

## Menu criativo vazio

O Bedrock pode mostrar o inventário criativo **inteiro** vazio quando as outras abas não têm itens — mesmo com Nature correto. É limitação da UI do cliente.

## Correção recomendada (plugin sample)

```bash
dotnet build plugins/orion:creative-fillers/OrionCreativeFillers.csproj
```

Ative em `config/server.json`:

```json
"Plugins": { "Enabled": true, "Directory": "plugins" }
```

## Vida / fome (attributes)

```bash
dotnet build plugins/orion:attributes/OrionAttributes.csproj
```

Com plugins ativos: `provides: orion:attributes`, `orion:health`, `orion:hunger`. Consuma via `IAttributesApi` / `IEntityHealthService` / `IPlayerHungerService`. Prefira carregar com `orion:inventory` (softdepend).

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
