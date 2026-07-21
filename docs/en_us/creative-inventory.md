# Creative inventory (Bedrock)

## Orion model

- **ItemRegistry**: full vanilla palette (`item_types.json`) — the client replaces its item table with this packet.
- **CreativeContent**: curated menu — empty by default (`orion/items.json` is `[]`); all tabs via `ICreativeTabRegistry.AddEntry` (typically in `IOrionPlugin.Load`, e.g. `orion:minimal-items`).

## Client minimum requirement

Bedrock’s creative UI indexes entries by **category** (Construction, Nature, Equipment, Items).

If Construction / Equipment / Items are empty, the client often shows the **entire** inventory as empty even when Nature is correct.

| Tab | Default source | Content |
|-----|----------------|---------|
| Construction | (empty) / `orion:minimal-items` | e.g. cobblestone |
| Nature | (empty) / `orion:minimal-items` | e.g. grass, dirt, bedrock |
| Equipment | (empty) / `orion:minimal-items` | e.g. wooden sword |
| Items | (empty) / `orion:minimal-items` | e.g. stick |

Orion **does not** load plugins by default. Without fillers, boot logs a warning pointing to [first-run.md](first-run.md).

## Sample plugin `orion:minimal-items`

Repo: [OrionBedrock/orion-minimal-items](https://github.com/OrionBedrock/orion-minimal-items).

C# assembly implementing `IOrionPlugin` that registers the six host blocks, Nature entries, and three fillers in `Load()`. Enable with `Plugins.Enabled: true` after building the project.

Architecture roadmap (McMaster, events, registries, packet hooks, …): [plugins/README.md](plugins/README.md).

## Adding creative entries

Call `CreativeTabs.AddEntry(pluginId, category, identifier)` in `Load` (categories 1–4). Identifiers must exist in the vanilla item palette.
