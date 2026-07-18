# Inventário criativo (Bedrock)

## Modelo Orion

- **ItemRegistry**: palette vanilla completa (`item_types.json`) — o cliente substitui a tabela de itens por esse pacote.
- **CreativeContent**: menu curado — blocos do mundo em **Nature** (`orion/items.json`); outras abas só via `ICreativeTabRegistry.AddEntry` (tipicamente em `IOrionPlugin.Load`).

## Requisito mínimo do cliente

O menu criativo do Bedrock indexa itens por **categoria** (Construction, Nature, Equipment, Items).

Se Construction / Equipment / Items estiverem vazias, o cliente frequentemente mostra o inventário **inteiro vazio**, mesmo com Nature correta.

| Aba | Origem padrão | Conteúdo |
|-----|---------------|----------|
| Construction | (vazio) / plugin opt-in | ex.: pedregulho |
| Nature | `orion/items.json` | blocos ativos do servidor |
| Equipment | (vazio) / plugin opt-in | ex.: espada de madeira |
| Items | (vazio) / plugin opt-in | ex.: stick |

O Orion **não** carrega plugins por padrão. Sem fillers, o boot emite um aviso apontando para [first-run.md](first-run.md).

## Plugin de exemplo `MinimalInventoryItems`

Pasta: [`plugins/MinimalInventoryItems/`](../../plugins/MinimalInventoryItems/).

É um assembly C# que implementa `IOrionPlugin` e registra os três fillers no `Load()`. Ative com `Plugins.Enabled: true` após compilar o projeto.

Roadmap de arquitetura (McMaster, eventos, registries, packet hooks, …): [plugins/README.md](plugins/README.md).

## Adicionar blocos do mundo

Edite `src/Protocol/Data/orion/items.json` (`creative: true` por padrão). Rebuild / reinicie o servidor.
