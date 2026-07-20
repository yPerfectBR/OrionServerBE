# Fase 18 — Checklist de implementação IA (SDK)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/18-sdk-ai-implementation-checklist.md`](../../en_us/plugins/18-sdk-ai-implementation-checklist.md)  
**Depende de:** [09](09-sdk-overview.md)–[17](17-sdk-vanilla-dogfood.md)

## 1. Goal

Ordem **estrita** de PRs/commits para construir o SDK final das fases 09–17 **sem arquiteturas temporárias**. Complementa o checklist de plataforma [08](08-ai-implementation-checklist.md).

## 2. Non-goals

- Reimplementar McMaster/lifecycle já `implemented` em 01–07.
- Caminhos de autoria “compilar contra Orion.dll”.

## 3. Sequência

| Passo | Doc | Trabalho | Critério de saída |
|-------|-----|----------|-------------------|
| S1 | [10](10-sdk-packages-versioning.md) | Projetos Api skeleton, pack, SharedAssemblies, validação `api` | pack ok; api inválida rejeitada |
| S2 | [11](11-sdk-orion-api-surface.md) | Facades + implementação; stubs removidos | IPlayer usável de plugin Api |
| S3 | [12](12-sdk-registries-traits.md) | Registries ricos + traits | Bloco+trait no Load |
| S4 | [13](13-sdk-events-signals.md) | Sinais em Orion.Api.Events + novos | Cancel place/eat |
| S5 | [14](14-sdk-gameplay-services.md) | Gameplay.Api + IPlayer | TryGet de plugin externo |
| S6 | [15](15-sdk-protocol-escape.md) | IOutboundPacket + helpers | Plugin sem Protocol atualiza bloco |
| S7 | [17](17-sdk-vanilla-dogfood.md) | Migrar Vanilla\*; remover IVT | Sem ref Orion.csproj |
| S8 | [16](16-sdk-external-plugin-guide.md) | Template + sample via NuGet pack | Restore limpo |
| S9 | Docs | Status `implemented` | Docs = código |

## 4. Definition of Done global

- Três pacotes mesma Version; SharedAssemblies; `api` enforced; zero ProjectReference plugins→Orion; zero IVT Vanilla; MinimalInventoryItems ok; sample deep NuGet-only; smoke Vanilla\*; testes verdes; docs 09–18 `implemented`.

## 5. Anti-padrões

- Novo InternalsVisibleTo para plugin  
- Share Orion.dll / Protocol no McMaster  
- Gameplay.Api público com `Player` / `DataPacket` concreto  
- DevKit HintPath  
- Stubs vazios IOrionServer/IOrionWorld  

## 6. Status

`spec` — só vira `implemented` com §4 completo.
