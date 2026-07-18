# Arquitetura de plugins Orion

**Status:** Fases 1–6 `implemented` (McMaster + events + registries + services + packet hooks). Demais fases `spec`.

Este hub descreve como o Orion vira uma **engine Bedrock mínima** cuja superfície de gameplay cresce com **plugins C# de terceiros**, carregados **exclusivamente** com **McMaster.NETCore.Plugins**, isolados por assembly load context e coordenados por contratos, eventos, registries, services, messaging e (depois) packet hooks.

English: [`../../en_us/plugins/README.md`](../../en_us/plugins/README.md)

## Decisões travadas

| Tópico | Decisão |
|--------|----------|
| Loader | **Exclusivo** [McMaster.NETCore.Plugins](https://github.com/natemcmaster/DotNetCorePlugins) 2.x — proibido `Assembly.LoadFrom`, ALC próprio ou scan de DLL sem McMaster |
| Contratos | Assembly fino `Orion.PluginContracts` — plugins **não** referenciam o monolito Orion |
| Inter-plugin | Services registry (estilo Bukkit) + message bus namespaced (`plugin:channel`) |
| Conflitos | Prioridades, cancel/replace, ownership de registry, `provides` / `softdepend` — sem merge mágico |
| Packet hooks | Sim — fase dedicada (estilo Endstone / PocketMine) |
| Runtime | Host **managed** com plugins (não Native AOT) |

## Pipeline de boot (alvo)

```mermaid
flowchart TB
  subgraph boot [Boot]
    Config[Plugins.Enabled]
    Loader[McMaster PluginLoader]
    Manifest[plugin.json resolve order]
    Load[Plugin.Load contracts only]
    ServerBoot[ServerHost.Bootstrap]
    Enable[Plugin.OnEnable subscribe]
    WorldInit[OnWorldInitialize registries]
  end
  Config --> Loader --> Manifest --> Load --> ServerBoot --> Enable --> WorldInit

  subgraph runtime [Runtime]
    Events[Typed EventBus priorities]
    Services[ServicesRegistry]
    Bus[Namespaced MessageBus]
    Packets[Packet Receive Send hooks]
    CoreHandlers[Core packet handlers]
  end
  Enable --> Events
  Enable --> Services
  Enable --> Bus
  Packets --> CoreHandlers
  Packets --> Events
```

## Mapa de fases

| Fase | Doc | Objetivo | Status |
|------|-----|----------|--------|
| 0 | [00 — Visão / engine mínima](00-vision-minimal-engine.md) | O que fica no core vs plugins | `spec` |
| 1 | [01 — Loader e contratos (McMaster)](01-loader-contracts-mcmaster.md) | Isolamento, shared types, layout | `implemented` |
| 2 | [02 — Lifecycle e manifest](02-lifecycle-manifest.md) | Load / Enable / WorldInitialize; `plugin.json` | `implemented` |
| 3 | [03 — Eventos e prioridades](03-events-priorities.md) | Expor bus tipado aos plugins | `implemented` |
| 4 | [04 — Registries e conteúdo](04-registries-content.md) | Itens, blocos, comandos, creative tabs | `implemented` |
| 5 | [05 — Services e messaging](05-services-messaging.md) | Integração soft sem hard load deps | `implemented` |
| 6 | [06 — Packet hooks](06-packet-hooks.md) | Interceptação receive/send de baixo nível | `implemented` |
| 7 | [07 — Conflitos e compatibilidade](07-conflicts-compatibility.md) | Ferramentas quando plugins colidem | `implemented` |
| — | [08 — Checklist de implementação (IA)](08-ai-implementation-checklist.md) | Ordem de PRs, APIs, testes de aceitação | `spec` |

**Implementado (PR 1–7):** McMaster, lifecycle, registries, events, services/messenger, `IPacketPipeline`, diagnostics de conflitos (`ConflictMode` / `/plugins`). Ver [first-run](../first-run.md).

## Glossário

| Termo | Significado |
|-------|-------------|
| **Core / engine** | Rede, persistência de mundo/chunks, sessões, scheduling, codecs de protocolo, conteúdo curado mínimo |
| **Plugin** | Assembly C# publicado em `plugins/<Id>/` implementando `IOrionPlugin` |
| **Contratos** | `Orion.PluginContracts` — tipos estáveis compartilhados entre ALCs |
| **Hard depend** | Manifest `depend` — boot falha se ausente |
| **Soft depend** | Manifest `softdepend` — só reordena; descoberta em runtime via Services / Messenger |
| **Provides** | Capacidade nomeada para discovery |
| **Ownership de registry** | No máximo um plugin “dona” uma chave (identifier ou PacketId) |
| **Escape hatch** | Packet hooks quando ainda não há evento/API de alto nível |

## Docs relacionados

- [First run](../first-run.md)
- [Inventário criativo](../creative-inventory.md)
- [Filosofia e arquitetura](../architecture-philosophy.md)
- [Status do projeto](../project-status.md)

## Inspiração externa (citada nas fases)

- Paper / Bukkit — events, ServicesManager, softdepend
- PocketMine-MP — `DataPacketReceiveEvent`, depend/softdepend no plugin.yml
- Endstone — `PacketReceiveEvent` / `PacketSendEvent` no Bedrock
- SerenityJS / the-aether — `onInitialize` / `onWorldInitialize` + palettes
- McMaster DotNetCorePlugins — isolamento ALC e `sharedTypes`
