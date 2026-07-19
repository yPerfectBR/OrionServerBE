# First run (OrionServer)

O Orion sobe com um menu criativo **mínimo**: só Nature a partir de `src/Protocol/Data/orion/items.json` (ex.: grass, dirt, bedrock). Construction, Equipment e Items ficam vazias até um plugin registrar fillers.

## Menu criativo vazio

O Bedrock costuma mostrar o inventário criativo **inteiro** vazio quando essas outras abas não têm itens — mesmo com Nature correta. É limitação da UI do cliente, não falha do pacote de Nature.

No boot, se essas abas ainda estiverem vazias, o Orion registra um aviso apontando para cá.

## Correção recomendada (plugin de exemplo)

1. Compile o sample (gera `plugins/MinimalInventoryItems/MinimalInventoryItems.dll` ao lado de `plugin.json`):

```bash
dotnet build plugins/MinimalInventoryItems/MinimalInventoryItems.csproj
```

2. Ative em `config/server.json` (o padrão é **desligado**):

```json
"Plugins": {
  "Enabled": true,
  "Directory": "plugins"
}
```

3. Reinicie. O host carrega o plugin **somente via McMaster** (pasta com `plugin.json`). O sample registra:

- Construction → pedregulho  
- Equipment → espada de madeira  
- Items → stick  

**Nota:** host com plugins = **managed** (.NET), não Native AOT.

## Vida / fome (plugin de atributos)

O core **não** inclui vida nem fome de gameplay. Para comportamento vanilla:

```bash
dotnet build plugins/VanillaAttributes/VanillaAttributes.csproj
```

Com `Plugins.Enabled: true`, o plugin registra traits + serviços (`provides: orion:attributes`, `orion:health`, `orion:hunger`). Outros plugins consomem via `IVanillaAttributesApi` / `IEntityHealthService` / `IPlayerHungerService`. Sem ele, não há HP/fome/comida. Preferível carregar junto com `VanillaInventory` (softdepend).

## Inventário, containers, construção e mineração

O core **não** inclui inventário do jogador, baú/barril, place de bloco nem mining. Build:

```bash
dotnet build plugins/VanillaInventory/VanillaInventory.csproj
dotnet build plugins/VanillaContainers/VanillaContainers.csproj
dotnet build plugins/VanillaBuilding/VanillaBuilding.csproj
dotnet build plugins/VanillaMining/VanillaMining.csproj
```

Dica de carga: Inventory → Building / Mining (softdepends). Survival place/mine precisa do inventário; creative place funciona só com Building.

- `VanillaInventory` — inventário/cursor/ISR (`provides: orion:inventory`); evento cancelável `PlayerOpenInventorySignal`
- `VanillaContainers` — baú/barril com **`depend: ["VanillaInventory"]`**; evento `PlayerOpenContainerSignal`
- `VanillaBuilding` — place / use-on-block (`provides: orion:building`, softdepend Inventory); `IPlayerBlockUseHandler` / `IVanillaBuildingApi`
- `VanillaMining` — crack / destroy (`provides: orion:mining`, softdepend Inventory); `IPlayerBlockBreakHandler` / `IVanillaMiningApi`
- API inventário: `IVanillaInventoryApi` / `IPlayerInventoryService`

## Fillers próprios

No `IOrionPlugin.Load(IPluginLoadContext)`, use `context.Registries.CreativeTabs.AddEntry(pluginId, category, identifier)` **antes** do init do catálogo (o boot do servidor já ordena isso). Não coloque Nature (categoria 2) aí — edite `orion/items.json`.

Mais: [creative-inventory.md](creative-inventory.md) · [plugins/README.md](plugins/README.md).
