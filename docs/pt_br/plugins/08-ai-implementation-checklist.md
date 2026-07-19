# Fase 8 — Checklist de implementação (IA)

**Status:** `spec`  
**Twin:** [`../../en_us/plugins/08-ai-implementation-checklist.md`](../../en_us/plugins/08-ai-implementation-checklist.md)

Ordem de implementação para agentes e humanos. Cada fase mapeia aos docs `01`–`07`. Não pular dependências. Nomes de tipos/APIs iguais nos twins EN/PT.

## Regras globais

1. `Plugins.Enabled` default **`false`**.
2. Plugins referenciam **`Orion.PluginContracts`**, não o monolito Orion.
3. **Loader exclusivo McMaster** — proibido `Assembly.LoadFrom` / ALC caseiro.
4. Testes em `tests/` por fase.
5. Atualizar Status `spec` → `implemented` nos **dois** idiomas quando a aceitação passar.
6. Publish managed para builds com plugins (sem Native AOT + plugins dinâmicos).

---

## PR 1 — Contratos + loader McMaster (Fases 1–2)

### Implementar

- [x] `src/PluginContracts/` com `IOrionPlugin`, contexts, manifest.
- [x] Package McMaster no host Orion.
- [x] Reescrever [`PluginHost`](../../../src/Orion/Plugins/PluginHost.cs) (McMaster + `plugin.json` + topo-sort).
- [x] Boot: Load → catálogo → Bootstrap → Enable → WorldInitialize.
- [x] Migrar MinimalInventoryItems (contracts + publish + `plugin.json`).
- [x] Perfil AOT vs plugins documentado.

### Aceitação

- [x] §6 de [01](01-loader-contracts-mcmaster.md) e [02](02-lifecycle-manifest.md).

---

## PR 2 — Event bus e prioridades (Fase 3)

- [x] `IEventBus` + `EventPriority`; adapter em [`Server`](../../../src/Orion/Server.cs).
- [x] Unsubscribe + cleanup no disable; `ICancellable` normalizado.
- [x] Teste/sample em `PlayerChatSignal`.

Aceitação: [03](03-events-priorities.md) §6 — ok.

---

## PR 3 — Registries de conteúdo (Fase 4)

- [x] Facades `IContentRegistries` sobre catálogo/blocos/comandos/generators.
- [x] Freeze após payloads; MinimalInventoryItems usa `ICreativeTabRegistry`.

Aceitação: [04](04-registries-content.md) §6 + testes curados existentes — ok.

---

## PR 4 — Services + Messenger (Fase 5)

- [x] `ServiceRegistry`, `PluginMessenger`, `/plugins` enriquecido.

Aceitação: [05](05-services-messaging.md) §6 — ok.

---

## PR 5 — Packet hooks (Fase 6)

- [x] Hooks no ingress/send; `TryOwnHandler`; warning subscribe-all.

Aceitação: [06](06-packet-hooks.md) §6 — ok.

---

## PR 6 — Diagnóstico de conflitos (Fase 7)

- [x] `ConflictMode`, `IPluginDiagnostics`, `/plugins` com conflicts.

Aceitação: [07](07-conflicts-compatibility.md) §6 — ok.

---

## Workflow sugerido por PR

1. Ler a fase (§1–8) + esta seção.
2. Implementar superfície mínima dos sketches.
3. `dotnet test` filtrado.
4. Atualizar sample se a API quebrou.
5. Marcar Status `implemented` em EN e PT-BR.
6. Só então a próxima PR.

## Fora de escopo

Marketplace; toolchain completa de RP de blocos custom; merge semântico multi-plugin; AOT + plugins.

## Mapa rápido

| Fase | Doc |
|------|-----|
| Visão | [00](00-vision-minimal-engine.md) |
| Loader | [01](01-loader-contracts-mcmaster.md) |
| Lifecycle | [02](02-lifecycle-manifest.md) |
| Eventos | [03](03-events-priorities.md) |
| Registries | [04](04-registries-content.md) |
| Services | [05](05-services-messaging.md) |
| Packets | [06](06-packet-hooks.md) |
| Conflitos | [07](07-conflicts-compatibility.md) |
