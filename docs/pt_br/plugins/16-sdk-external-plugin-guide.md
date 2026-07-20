# Fase 16 — Guia do autor de plugin externo (final)

**Status:** `spec`  
**Language twin:** [`../../en_us/plugins/16-sdk-external-plugin-guide.md`](../../en_us/plugins/16-sdk-external-plugin-guide.md)  
**Depende de:** [09](09-sdk-overview.md)–[15](15-sdk-protocol-escape.md)

## 1. Goal

Guia **final** ponta a ponta: template, `plugin.json`, layout de publish e walkthroughs dos use-cases. Autores **não** clonam o monorepo Orion.

## 2. Non-goals

- Ensinar C# / .NET.
- Compatibilidade binária entre majors sem recompilar.

## 3. Template (final)

`plugin.json` com id/version/main; csproj com PackageReference a `Orion.PluginContracts`, `Orion.Api`, `Orion.Gameplay.Api` usando `ExcludeAssets=runtime` + `PrivateAssets=all`; target que copia a DLL ao lado do `plugin.json`.

Layout:

```
plugins/MyPlugin/
  plugin.json
  MyPlugin.dll
```

## 4. Walkthroughs

**A — Shop soft:** softdepend Economy + `TryGet<IEconomy>` + comando.  
**B — Kit minigame:** `PlayerJoinSignal` + `IPlayerInventoryService.TryClear` / `TryGive` + `SendMessage`.  
**C — Ore custom:** `BlockRegistration` rico + `BlockTraits.RegisterFromAssembly` + `PlayerBreakBlockSignal` com drop via inventário.  
**D — Cancel place alto:** `PlayerPlaceBlockSignal.Cancel()`.  
**E — Observar packets:** `OnReceive` em Monitor; sem roubar ownership do VanillaInventory.

## 5. Checklist do autor

- pasta == id == assembly; sem copiar Orion.Api.dll; soft via `TryGet`; testar contra build publicado do servidor.

## 6. File touch list

`templates/OrionPlugin/`; sample opcional `plugins/examples/DeepOreSample/`.

## 7. Acceptance tests

- Máquina limpa: restore → build → load no servidor; kit e ore funcionam sem source do Orion.

## 8. Status

`spec` — **auditoria jul/2026:** sem template `dotnet new`; restore só-NuGet **impossível** (pacotes não publicados); guia [20](20-plugin-developer-guide.md) cobre layout/migração atual.
