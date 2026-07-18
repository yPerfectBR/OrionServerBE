# Fase 7 — Conflitos e compatibilidade

**Status:** `implemented`  
**Twin:** [`../../en_us/plugins/07-conflicts-compatibility.md`](../../en_us/plugins/07-conflicts-compatibility.md)

## 1. Objetivo

Documentar **conflitos inevitáveis** quando dois plugins tocam o mesmo recurso e oferecer **ferramentas** para detectar, ordenar e conter — sem prometer merge semântico automático.

## 2. Não-objetivos

- Fundir duas economias `IEconomy` num ledger único.
- Patch estilo Harmony entre plugins.
- Compatibilidade garantida entre versões arbitrárias de terceiros.

## 3. Esboço de API pública

```json
"Plugins": {
  "Enabled": false,
  "Directory": "plugins",
  "ConflictMode": "warn"
}
```

`ConflictMode`: `warn` (default) | `fail`.

```csharp
public interface IPluginDiagnostics
{
    IReadOnlyList<PluginConflict> Conflicts { get; }
    IReadOnlyList<IPluginManifest> LoadedManifests { get; }
}

public sealed record PluginConflict(
    string Kind,
    string Key,
    string WinnerPluginId,
    string LoserPluginId,
    string Message);
```

### `/plugins` estendido

Lista ids, `provides`, softdepend resolvido e conflitos WARN.

## 4. Sequência / pontos de detecção

1. Ids duplicados / ciclos — **sempre fail**.
2. Registry double-register — `ConflictMode`.
3. Service duplo — permitido; prioridade escolhe; diagnostic.
4. Packet ownership — segundo falha.
5. “Guerra” de cancel em eventos — disciplina de prioridade (não auto-detect).

## 5. Arquivos a tocar

`PluginsConfig.ConflictMode`; emitir conflitos em registries/services/packets; [`PluginsCommand`](../../../src/Orion/Commands/List/Operator/Plugins.cs); logs estruturados.

## 6. Testes de aceitação

- Creative id duplicado ⇒ warn/fail.
- Dois `IEconomy` ⇒ `TryGet` = maior prioridade; `/plugins` mostra ambos.
- Ownership de packet: segundo false + conflict entry.
- `fail` aborta boot em clash de registry.

## 7. Notas de migração

Hoje o primeiro registro vence em silêncio. Fase 7 torna isso explícito.

## 8. Status

`implemented`

## Matriz de ferramentas

| Problema | Ferramenta |
|----------|------------|
| Integração opcional | softdepend + TryGet + Messenger |
| Integração obrigatória | depend |
| Discovery | provides + `/plugins` |
| Mesmo evento | EventPriority + Cancel |
| Mesmo item/bloco | Ownership + ConflictMode |
| Mesmo packet | TryOwnHandler |
| Mesmo service | ServicePriority |
| Só observar | Monitor |

## O que continua inevitável

Dois plugins com políticas opostas de chat; duas simulações de projétil; skew de versão em `Foo.Api`.

## Compatibilidade sem acoplar load

Shop compila contra `Economy.Api`, não contra a implementação; softdepend só reordena quando ambos existem.
