# OrionServer

OrionServer is a custom Minecraft Bedrock server software focused on a **DIY (Do It Yourself)** philosophy.

Instead of shipping a large vanilla feature set by default, OrionServer aims to provide only the minimal core required for a server to exist (player lifecycle, chunk rendering, essential networking, and foundational runtime systems). Everything else should be opt-in through project-specific implementation or plugins.

## Project Philosophy

- **Minimal core, opt-in features**: add only what your server actually needs.
- **Not a vanilla BDS clone**: this project does not try to fully mirror Bedrock Dedicated Server behavior.
- **Built for specialized workloads**: fixed maps, minigames, RPG scenarios, apocalypse-style worlds, and other custom experiences.
- **Performance and scale first**: especially for many players and heavy systems in the same world.

## Why OrionServer

Many custom server ecosystems scale by splitting players across isolated experiences. OrionServer also targets cases where you want large-scale activity in a shared world and need stronger CPU utilization through multithreading-oriented architecture.

The long-term direction includes better multithreading and async/job tooling for plugin developers, so server-side systems can scale with modern multi-core processors.

## Current Status

OrionServer is currently **unstable** and under active architectural evolution.

- Recommended for testing, experimentation, and contributors who want to help build a solid foundation.
- Not recommended yet for serious production servers unless you are prepared to maintain compatibility with future breaking changes.

If you want a mostly vanilla survival stack with high control, consider projects built on top of official BDS (for example, Endstone).

## Documentation

- Main docs (English): [`docs/en_us`](docs/en_us/README.md)
- Portuguese docs: [`docs/pt_br`](docs/pt_br/README.md)

Additional languages can be added later using the same folder structure.

## Contributing

Please read contribution rules before opening pull requests or issues:

- PR guidelines: [`docs/en_us/contributing/pr-guidelines.md`](docs/en_us/contributing/pr-guidelines.md)
- Issue guidelines: [`docs/en_us/contributing/issue-guidelines.md`](docs/en_us/contributing/issue-guidelines.md)

## Credits

Special thanks to:

- [Basalt](https://github.com/BasaltBE/Basalt)
- [SerenityJS](https://github.com/SerenityJS/serenity)

Many ideas were inspired by these projects.
