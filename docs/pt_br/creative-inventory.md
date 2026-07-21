# Inventário criativo (Bedrock)

## Modelo Orion

- **ItemRegistry**: paleta vanilla completa (`item_types.json`) — o cliente substitui a tabela de itens com este pacote.
- **CreativeContent**: menu curado — vazio por padrão (`orion/items.json` é `[]`); todas as abas via `ICreativeTabRegistry.AddEntry` (tipicamente em `IOrionPlugin.Load`, ex. `orion:minimal-items`).

## Requisito mínimo do cliente

A UI criativa do Bedrock indexa entradas por **categoria** (Construction, Nature, Equipment, Items).

Se Construction / Equipment / Items estiverem vazias, o cliente muitas vezes mostra o inventário **inteiro** vazio mesmo com Nature correto.

| Aba | Fonte padrão | Conteúdo |
|-----|--------------|----------|
| Construction | (vazio) / `orion:minimal-items` | ex. cobblestone |
| Nature | (vazio) / `orion:minimal-items` | ex. grass, dirt, bedrock |
| Equipment | (vazio) / `orion:minimal-items` | ex. wooden sword |
| Items | (vazio) / `orion:minimal-items` | ex. stick |

Orion **não** carrega plugins por padrão. Sem fillers, o boot registra um warning apontando para [first-run.md](first-run.md).

## Plugin sample `orion:minimal-items`

Repo: [OrionBedrock/orion-minimal-items](https://github.com/OrionBedrock/orion-minimal-items).

Assembly C# com `IOrionPlugin` que registra os seis blocos do host, Nature e três fillers em `Load()`. Ative com `Plugins.Enabled: true` após o build.

Roadmap de arquitetura: [plugins/README.md](plugins/README.md).

## Adicionando entradas criativas

Chame `CreativeTabs.AddEntry(pluginId, category, identifier)` em `Load` (categorias 1–4). Os identifiers precisam existir na paleta vanilla.
