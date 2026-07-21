# Fase 28 — Conteúdo mínimo e core vazio

**Status:** `implemented`  
**Language twin:** [`../../en_us/plugins/28-minimal-content-and-empty-core.md`](../../en_us/plugins/28-minimal-content-and-empty-core.md)  
**Depends on:** [22](22-vanilla-extraction-overview.md), [23](23-extraction-sdk-prerequisites.md), [25](25-block-mechanics-plugins.md)  
**Code prerequisite:** `RegisterPluginBlock` / facades de itens estáveis; freeze do catálogo após `Load`

## 1. Objetivo

1. Remover **todos** os registers nativos de [`BlockRegistry.RegisterFromBedrockStates`](../../../src/Orion/Block/BlockRegistry.cs) (6 blocos).
2. Embarcar plugin(s) de conteúdo mínimo que re-registrem esses blocos (e itens / runtime ids relacionados).
3. Garantir ordem de load: **`air` (e o resto) registrados antes do world init**; generators `depend` desse conteúdo.

## 2. Não-objetivos

- Paleta vanilla completa.
- Stub permanente de `air` no core (proibido no estado final — ver §4).
- Superflat (fase [29](29-worldgen-superflat-plugin.md)).

## 3. Conteúdo migrado

| Identifier | Notas |
|------------|--------|
| `minecraft:air` | Obrigatório para chunks/protocolo; prioridade máxima de load |
| `minecraft:structure_void` | |
| `minecraft:bedrock` | hardness -1 |
| `minecraft:dirt` | |
| `minecraft:grass_block` | |
| `minecraft:barrier` | non-solid |

Também: Nature saiu de `orion/items.json`; allowlist de barrier/structure_void; fillers de Construction / Equipment / Items (ex-`creative-fillers`).

## 4. Plugin (entregue)

| id | PackageId | Repo | provides | depend | Papel |
|----|-----------|------|----------|--------|-------|
| `orion:minimal-items` | `Orion.Plugins.MinimalItems` | `orion-minimal-items` | `orion:minimal-items`, `orion:creative-tab-fillers` | — | Seis blocos + Nature + fillers |

**Escolha de entrega:** mono-plugin (renomeado de `orion:creative-fillers`). Separar em `minimal-blocks` + `minimal-items` depois se o footprint de itens crescer.

### Política de `air`

1. `orion:minimal-items` registra `air` no `plugin.Load` (antes do bootstrap de mundo).
2. `orion:superflat` declara `depend` `orion:minimal-items`.
3. Core **não** faz `RegisterBlock` de conteúdo. `RegisterFromBedrockStates` é no-op.

## 5. Mudanças no core

- `RegisterFromBedrockStates` vazio.
- `orion/items.json` vazio; Nature (categoria 2) aceita de plugins.
- Allowlist de `/give` é autoritativa mesmo vazia.
- Testes usam `MinimalContentFixtures` quando McMaster não carrega.

## 6. Status

`implemented`
