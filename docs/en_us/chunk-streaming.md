# Chunk streaming (Bedrock)

This document explains how Orion streams terrain to Bedrock clients, why the negotiated radius is not the raw render-distance slider value, and which packets participate in the flow.

## Overview

The server streams a **Chebyshev square** (`max(|dx|, |dz|) ≤ viewDistance`).

The Bedrock client **culls** chunks outside a **circle** whose radius comes from `ChunkRadiusUpdated`.

If the server streams a square larger than the circle the client accepts, the corners disappear (jagged void) even when debug HUD shows `loaded=N/N`.

Therefore Orion:

1. Picks a Chebyshev streaming radius `R`.
2. Sends `ChunkRadiusUpdated` with `squareToCircle(R)` (same padding as Geyser).
3. Publishes `NetworkChunkPublisherUpdate` with block radius `squareToCircle(R) << 4`.
4. Never sends `squareToCircle(R)` above the client's `MaxChunkRadius` (crashes on some devices). Instead it **reduces `R`**.

## Math (`ChunkViewMath`)

File: `src/World/Coordinates/ChunkViewMath.cs`

| Function | Role |
|----------|------|
| `SquareToCircle(R)` | `ceil((R + 1) * √2)` — Bedrock circular radius for Chebyshev stream `R` (Geyser `ChunkUtils.squareToCircle`). |
| `MaxChebyshevForClientCircle(clientMax)` | Largest `R` such that `SquareToCircle(R) ≤ clientMax`. |
| `PublisherRadiusBlocks(R)` | `SquareToCircle(R) << 4` — publisher radius in blocks. |

Examples (approx.):

| Client `MaxChunkRadius` | Max Chebyshev stream | `ChunkRadiusUpdated` |
|-------------------------|----------------------|----------------------|
| 16 | 10 | 16 |
| 28 | 18 | 27 |
| 32 | 21 | 32 |

Asking for render distance 32 in the client does **not** guarantee a server stream of 32: the cap is whatever still fits the device circle.

## Radius negotiation

Handler: `src/Orion/Network/Handlers/RequestChunkRadius.cs`

When the client sends `RequestChunkRadius`:

1. Read `ChunkRadius` (requested) and `MaxChunkRadius` (device limit).
2. `maxChebyshev = MaxChebyshevForClientCircle(clientMax)`.
3. `R = clamp(requested, 4, min(server MaxViewDistance, maxChebyshev))`.
4. Send `UpdateChunkRadius` (`ChunkRadiusUpdated`) with `SquareToCircle(R)`.
5. Call `IPlayerChunkView.ApplyViewDistance(R)` (plugin trait `orion:player-chunk-rendering`).

Changing render distance **while already logged in** re-runs this path. The high-distance void was not hot-reload itself: it was clamping only the circle to `clientMax` while keeping a Chebyshev stream that was too large.

## Publisher (`NetworkChunkPublisherUpdate`)

Trait: plugin `orion:player-chunk-rendering` (`PlayerChunkRenderingTrait`, Api `IPlayerChunkView`).

- Position = player's current block (X/Y/Z).
- `Radius` = `PublisherRadiusBlocks(ViewDistance)`.
- `SavedChunks` = always empty (only meaningful with Client-Side Generation; Orion has CSG off).
- Updates on **every player chunk change** (not every 2+ chunks), matching Geyser.
- On the session tick, the publisher is sent **before** `LevelChunk` packets when the view center moved.

Encoding (protocol ≥ 937): Y is **ZigZag** (`BlockPos`), not unsigned `VarUInt`. See `NetworkChunkPublisherUpdate.cs`.

`ChunkRadiusUpdated` also uses ZigZag (`UpdateChunkRadius.cs`).

## Sending `LevelChunk`

- Spiral / Chebyshev ring scan up to `ViewDistance`.
- Up to 64 chunks per session tick.
- Unload out-of-range chunks with an empty `LevelChunk` (`SubChunkCount = 0`) to clear the client.

### Network biome payload

On network serialization (`Chunk.Serialize`, `nbt: false`):

- Biomes are written for the **full** dimension height (`MaxSubChunks`), not only non-empty sections.
- Network biome headers use the non-persistence bit `| 1` (same pattern as block palettes / PMMP).
- SuperFlat fills plains biome (`1`) on generated sections.

Without this, the client can misread the payload and fail to render terrain even when chunks were “sent”.

## Area handoff

Same-worker and cross-worker share the same client path: `ResyncAfterRegionHandoff` → `AfterRegionHandoff()` (publisher / presence), **without** a forced unload just for a thread switch.

Ownership and peer details: [area-threading.md](area-threading.md). After `/tp` with a full reload, streaming still waits on the hold (`_awaitingTeleportChunkSync`). See [teleport.md](teleport.md).

## Debug

The tip HUD (`orion:player-debug`) may show a line from `FormatDebugHudLine()`:

```text
view vd=18 pub=432b chunk=(6,-1) loaded=1369/1369 req=0 ready=0 scan=19/18 started=True
```

- `vd` = Chebyshev streaming radius (not the raw client slider).
- `pub` = publisher radius in blocks.
- `loaded=A/B` = chunks the server considers sent for the current view.

A full `loaded` count does **not** prove the client is displaying everything: if `ChunkRadiusUpdated` is wrong, the client still drops corners.

## Checklist when changing this flow

1. `SquareToCircle(stream) ≤` client `MaxChunkRadius`.
2. Publisher and `ChunkRadiusUpdated` use the **same** circular padding as the stream.
3. `SavedChunks` empty while CSG is off.
4. Full-height network biomes + network flag.
5. Publisher Y as ZigZag.
6. Test low (8/16) **and** high (28/32) render distance on the same device, including changing the slider while logged in.
