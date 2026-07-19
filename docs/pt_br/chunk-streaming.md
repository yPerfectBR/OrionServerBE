# Streaming de chunks (Bedrock)

Este documento descreve como o Orion envia terreno ao cliente Bedrock, por que o raio negociado não é o mesmo valor do slider de render distance, e quais pacotes participam do fluxo.

## Visão geral

O servidor faz streaming em **quadrado Chebyshev** (`max(|dx|, |dz|) ≤ viewDistance`).

O cliente Bedrock **descarta** chunks fora de um **círculo** cujo raio vem de `ChunkRadiusUpdated`.

Se o servidor mandar um quadrado maior do que o círculo que o cliente aceita, os cantos somem (void irregular), mesmo com `loaded=N/N` no HUD de debug.

Por isso o Orion:

1. Escolhe um raio Chebyshev de streaming `R`.
2. Envia `ChunkRadiusUpdated` com `squareToCircle(R)` (mesmo padding do Geyser).
3. Publica `NetworkChunkPublisherUpdate` com raio em blocos `squareToCircle(R) << 4`.
4. Nunca envia `squareToCircle(R)` acima do `MaxChunkRadius` do cliente (crash em alguns devices). Em vez disso, **reduz `R`**.

## Matemática (`ChunkViewMath`)

Arquivo: `src/World/Coordinates/ChunkViewMath.cs`

| Função | Papel |
|--------|--------|
| `SquareToCircle(R)` | `ceil((R + 1) * √2)` — raio circular Bedrock para um stream Chebyshev `R` (Geyser `ChunkUtils.squareToCircle`). |
| `MaxChebyshevForClientCircle(clientMax)` | Maior `R` tal que `SquareToCircle(R) ≤ clientMax`. |
| `PublisherRadiusBlocks(R)` | `SquareToCircle(R) << 4` — raio do publisher em blocos. |

Exemplos (aprox.):

| `MaxChunkRadius` do cliente | Stream Chebyshev máximo | `ChunkRadiusUpdated` |
|-----------------------------|-------------------------|----------------------|
| 16 | 10 | 16 |
| 28 | 18 | 27 |
| 32 | 21 | 32 |

Pedir render 32 no cliente **não** garante stream 32 no servidor: o teto é o que ainda cabe no círculo do device.

## Negociação de raio

Handler: `src/Orion/Network/Handlers/RequestChunkRadius.cs`

Quando o cliente manda `RequestChunkRadius`:

1. Lê `ChunkRadius` (pedido) e `MaxChunkRadius` (limite do device).
2. `maxChebyshev = MaxChebyshevForClientCircle(clientMax)`.
3. `R = clamp(pedido, 4, min(MaxViewDistance do servidor, maxChebyshev))`.
4. Envia `UpdateChunkRadius` (`ChunkRadiusUpdated`) com `SquareToCircle(R)`.
5. Chama `PlayerChunkRenderingTrait.ApplyViewDistance(R)`.

Mudar a render distance **já logado** reexecuta esse fluxo. O bug em distâncias altas não era o hot-reload em si: era cortar só o círculo em `clientMax` enquanto o stream Chebyshev continuava grande demais.

## Publisher (`NetworkChunkPublisherUpdate`)

Trait: `src/Orion/Player/Traits/PlayerChunkRenderingTrait.cs`

- Posição = bloco atual do jogador (X/Y/Z).
- `Radius` = `PublisherRadiusBlocks(ViewDistance)`.
- `SavedChunks` = sempre vazio (só faz sentido com Client-Side Generation; Orion tem CSG desligado).
- Atualiza a **cada mudança de chunk** do jogador (não a cada 2+ chunks), alinhado ao Geyser.
- No tick de sessão, o publisher sai **antes** dos `LevelChunk` quando o centro da view mudou.

Encoding (protocolo ≥ 937): Y é **ZigZag** (`BlockPos`), não `VarUInt` unsigned. Ver `NetworkChunkPublisherUpdate.cs`.

`ChunkRadiusUpdated` também usa ZigZag (`UpdateChunkRadius.cs`).

## Envio de `LevelChunk`

- Scan espiral / anéis Chebyshev até `ViewDistance`.
- Até 64 chunks por tick de sessão.
- Unload fora do raio com `LevelChunk` vazio (`SubChunkCount = 0`) para limpar o cliente.

### Payload de biomas (rede)

Na serialização de rede (`Chunk.Serialize`, `nbt: false`):

- Biomas são escritos para **toda** a altura da dimensão (`MaxSubChunks`), não só seções com blocos.
- Header de biome na rede usa o bit de não-persistência `| 1` (mesmo padrão de paleta de blocos / PMMP).
- SuperFlat preenche biome plains (`1`) nas seções geradas.

Sem isso o cliente pode interpretar o payload errado e falhar ao renderizar terreno mesmo com chunks “enviados”.

## Handoff entre áreas

Same-worker e cross-worker usam o mesmo caminho de cliente: `ResyncAfterRegionHandoff` → `AfterRegionHandoff()` (publisher / presença), **sem** unload forçado só por trocar de thread.

Detalhes de ownership e peers: [area-threading.md](area-threading.md). Após `/tp` com full reload, o streaming ainda espera o hold (`_awaitingTeleportChunkSync`). Ver [teleport.md](teleport.md).

## Debug

O tip HUD (`DebugTrait`) pode mostrar uma linha de `FormatDebugHudLine()`:

```text
view vd=18 pub=432b chunk=(6,-1) loaded=1369/1369 req=0 ready=0 scan=19/18 started=True
```

- `vd` = raio Chebyshev de streaming (não o slider bruto do cliente).
- `pub` = raio do publisher em blocos.
- `loaded=A/B` = chunks que o servidor considera enviados na view atual.

`loaded` cheio **não** prova que o cliente está mostrando tudo: se `ChunkRadiusUpdated` estiver errado, o cliente ainda descarta cantos.

## Checklist ao alterar este fluxo

1. `SquareToCircle(stream) ≤ MaxChunkRadius` do cliente.
2. Publisher e `ChunkRadiusUpdated` usam o **mesmo** padding circular da stream.
3. `SavedChunks` vazio enquanto CSG estiver off.
4. Biomas de rede em altura completa + flag de rede.
5. Y do publisher em ZigZag.
6. Testar render baixa (8/16) **e** alta (28/32) no mesmo device, inclusive mudando o slider já logado.
