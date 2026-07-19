# Creative inventory (Bedrock)

## Orion model

- **ItemRegistry**: full vanilla palette (`item_types.json`) — the client replaces its item table with this packet.
- **CreativeContent**: curated menu — world blocks in **Nature** (`orion/items.json`); other tabs only via `ICreativeTabRegistry.AddEntry` (typically in `IOrionPlugin.Load`).

## Client minimum requirement

Bedrock’s creative UI indexes entries by **category** (Construction, Nature, Equipment, Items).

If Construction / Equipment / Items are empty, the client often shows the **entire** inventory as empty even when Nature is correct.

| Tab | Default source | Content |
|-----|----------------|---------|
| Construction | (empty) / opt-in plugin | e.g. cobblestone |
| Nature | `orion/items.json` | active server blocks |
| Equipment | (empty) / opt-in plugin | e.g. wooden sword |
| Items | (empty) / opt-in plugin | e.g. stick |

Orion **does not** load plugins by default. Without fillers, boot logs a warning pointing to [first-run.md](first-run.md).

## Sample plugin `MinimalInventoryItems`

Folder: [`plugins/MinimalInventoryItems/`](../../plugins/MinimalInventoryItems/).

This is a C# assembly implementing `IOrionPlugin` that registers the three fillers in `Load()`. Enable with `Plugins.Enabled: true` after building the project.

Architecture roadmap (McMaster, events, registries, packet hooks, …): [plugins/README.md](plugins/README.md).

## Adding world blocks

Edit `src/Protocol/Data/orion/items.json` (`creative: true` by default). Rebuild / restart the server.
