# Fase 2 — Lifecycle e manifest

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/02-lifecycle-manifest.md`](../../en_us/plugins/02-lifecycle-manifest.md)

## 1. Objetivo

Definir um lifecycle determinístico e um manifest **`plugin.json`** para ordenar loads, falhar cedo em hard deps ausentes e soft-ordenar integrações opcionais — sem o assembly do plugin A hard-referenciar o B.

## 2. Não-objetivos

- Install remoto / marketplace.
- Hot-reload sem reiniciar o processo.
- Ranges SemVer complexos além de checagem simples de `api` major na v1.

## 3. Esboço de API pública

### `plugin.json`

```json
{
  "id": "MinimalInventoryItems",
  "version": "1.0.0",
  "api": "0.1.0",
  "description": "Preenche abas criativas não-Nature",
  "authors": ["Orion"],
  "main": "MinimalInventoryItems.MinimalInventoryItemsPlugin",
  "depend": [],
  "softdepend": [],
  "loadbefore": [],
  "provides": ["orion:creative-tab-fillers"]
}
```

| Campo | Obrigatório | Significado |
|-------|-------------|-------------|
| `id` | sim | Id único; casa com a pasta |
| `version` | sim | SemVer do plugin |
| `api` | sim | Versão mínima da PluginContracts API |
| `main` | sim | Tipo FQCN de `IOrionPlugin` |
| `depend` | não | Hard deps — ausente ⇒ erro de boot |
| `softdepend` | não | Se existir, carrega antes (só reorder) |
| `loadbefore` | não | Pedir load antes dos ids listados |
| `provides` | não | Capacidades para discovery |

Inspirado em PocketMine / Endstone.

### Lifecycle

```csharp
public enum PluginState
{
    Discovered,
    Loaded,
    Enabled,
    WorldReady,
    Disabled
}
```

| Método | Quando | Permitido |
|--------|--------|-----------|
| `Load` | Após ALC, **antes** do Server | Pré-catálogo, config; sem `Server.On` |
| `OnEnable` | Após `ServerHost.Bootstrap` | Eventos, comandos, services, messenger |
| `OnWorldInitialize` | Mundo/dimensões prontos | Palettes, generators |
| `OnDisable` | Shutdown | Unsubscribe, flush |

## 4. Sequência de boot / runtime

1. Descobrir `plugins/*/plugin.json`.
2. Validar ids únicos e `api`.
3. Grafo: `depend` / `softdepend` satisfeito / `loadbefore`.
4. Ciclos ⇒ erro fatal claro.
5. Ordem: McMaster → `main` → `Load`.
6. Catálogo / bootstrap do server.
7. `OnEnable` na mesma ordem.
8. `OnWorldInitialize` na mesma ordem.
9. Shutdown: `OnDisable` em **ordem inversa**.

## 5. Arquivos a tocar

| Path | Mudança |
|------|---------|
| Tipos de manifest em contracts ou Orion | DTO + parser |
| [`PluginHost.cs`](../../../src/Orion/Plugins/PluginHost.cs) | Discovery + topo-sort |
| [`Program.cs`](../../../src/Server/Program.cs) | Split Load / Enable / WorldInit |
| [`ServerHost.cs`](../../../src/Orion/ServerHost.cs) | Hook WorldInit após pregen |
| Sample `plugin.json` | Adicionar |

## 6. Testes de aceitação

- Hard `depend` ausente falha o boot com id no erro.
- Soft ausente: plugin ainda habilita.
- Soft presente: dependência carrega primeiro.
- Ciclo em `depend` falha.
- `id` duplicado falha.
- `main` inválido falha o plugin (e hard-dependents).

## 7. Notas de migração

| Hoje | Alvo |
|------|------|
| Pasta + DLL | `plugin.json` obrigatório |
| Só `Load()` | Quatro métodos |
| Sem ordem | Topological sort |

## 8. Status

`spec`

## Algoritmo de ordenação (normativo)

1. Nós = plugins descobertos.
2. Para cada `depend` e cada `softdepend` **satisfeito**, aresta `dep → plugin`.
3. Para cada `loadbefore: [X]`, aresta `plugin → X` se X existe.
4. Kahn; resto ⇒ ciclo.
5. Empate: ordem alfabética por `id`.

## Soft vs hard (orientação)

- **`depend`**: só quando o plugin **não sobe** sem o outro.
- Preferir **`softdepend` + Services/Messenger`** (Fase 5) para opcional.
- **`provides`**: diagnóstico “capacidade X fornecida por Y”.
