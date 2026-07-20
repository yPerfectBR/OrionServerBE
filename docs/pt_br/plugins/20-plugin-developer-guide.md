# Fase 20 — Guia do desenvolvedor de plugins

**Status:** `implemented`  
**Versão em inglês:** [`../../en_us/plugins/20-plugin-developer-guide.md`](../../en_us/plugins/20-plugin-developer-guide.md)  
**Depende de:** [19 — Manifest v2](19-manifest-v2.md), [09 — SDK overview](09-sdk-overview.md)

## 1. Público

Autores de plugins Orion (terceiros ou first-party) após manifest v2 e APIs de gameplay neutras.

## 2. Ambiente

| Requisito | Valor |
|-----------|--------|
| SDK | .NET 10 |
| NuGet (alvo) | `Orion.PluginContracts`, `Orion.Api`, `Orion.Gameplay.Api` |
| PackageReference | `ExcludeAssets="runtime"` — o host fornece implementações via McMaster |
| Monorepo (hoje) | `ProjectReference` aos projetos SDK; sem `Orion.csproj` a longo prazo |

## 3. Layout do projeto

Ver [21 — Layout de repositório](21-plugin-repo-layout.md). Resumo:

```
plugins/orion:meu-plugin/
  plugin.json
  README.md
  OrionMeuPlugin.csproj
  src/
    OrionMeuPluginPlugin.cs
  orion.meu-plugin.dll
```

- Pasta **igual** ao `id` do manifest.
- `AssemblyName` = `id` com `:` → `.`.

## 4. Manifest v2 (referência rápida)

- **`depend`**: dura — alvo obrigatório; carrega antes; `versions: [min, max]` inclusivo.
- **`softdepend`**: opcional — `load: "after"` (padrão) ou `"before"`; ignorada se o alvo não existir.
- **`provides`**: capabilities — **não** são ids de plugin.
- **`api`**: versão mínima do host ([10](10-sdk-packages-versioning.md)).

## 5. Serviços e substituição

1. **Dono de capability** — maior `ServicePriority` em `Register<T>` + `provides`.
2. **Dono de PacketId** — `TryOwnHandler` exclusivo (primeiro ganha).
3. **Substituir inventário** — cancelar `PlayerOpenInventorySignal` ou `IPlayerInventoryService` sem carregar `orion:inventory`.
4. **`depend`** — ordem fixa; sem campo `load`.

Ver política em [14 — Gameplay services](14-sdk-gameplay-services.md).

## 6. Pipeline de pacotes

| Modo | API | Uso |
|------|-----|-----|
| Observar | `OnReceive` / `OnSend` | Métricas, logs |
| Possuir | `TryOwnHandler` | Um dono por PacketId |

Não roube ISR do `orion:inventory` — use serviços/eventos.

## 7. Logging (`LogCategory.Plugins`)

Descoberta, ordem de carga, McMaster, conflitos e validação de manifest usam **`Plugins`**.

`config/server.json`:

```json
"Plugins": {
  "Debug": true,
  "Info": true,
  "Warn": true,
  "Error": true,
  "Chat": false
}
```

## 8. Troubleshooting (falhas de boot)

| Código / sintoma | Causa provável | Correção |
|------------------|----------------|----------|
| `MANIFEST_REGEX` | `id`/pasta/`versions` inválidos | `orion:produto`; pasta = `id` |
| `DEPEND_MISSING` | `depend` sem alvo | Adicionar plugin ou usar `softdepend` |
| `VERSION_OUT_OF_RANGE` | Versão fora do intervalo | Ajustar versão ou range |
| `VERSION_CONSTRAINT_CONFLICT` | Ranges disjuntos no mesmo alvo | Alinhar dependências |
| `ORDER_CYCLE` | Ciclo no grafo | Corrigir `softdepend.load` |
| `API_MISMATCH` | `api` incompatível | Atualizar plugin ou host |
| Assembly não encontrado | DLL ≠ `id` com `:` → `.` | `AssemblyName` no csproj |
| Falha McMaster | Contratos desalinhados | Mesma versão PluginContracts |

## 9. Boas práticas

- `softdepend` para integração opcional; `depend` só quando indispensável.
- SemVer honesto nos ranges.
- Não cachear serviços após `OnDisable`.
- Desinscrever eventos em `OnDisable`.
- Tipos de integração em pacote **`SeuPlugin.Api`**.

## 10. Walkthroughs

[16 — Guia de plugin externo](16-sdk-external-plugin-guide.md).

## Relacionados

- [19 — Manifest v2](19-manifest-v2.md)
- [21 — Layout](21-plugin-repo-layout.md)
- [First run](../first-run.md)
