# VanillaAttributes

Plugin opt-in de vitais vanilla: **vida**, **fome** e consumo de comida.

Expõe serviços no host para outros plugins (`softdepend: ["VanillaAttributes"]` ou provides `orion:attributes`).

## Build

```bash
dotnet build plugins/VanillaAttributes/VanillaAttributes.csproj
```

Gera `VanillaAttributes.dll` ao lado de `plugin.json`.

## Config

```json
"Plugins": {
  "Enabled": true,
  "Directory": "plugins"
}
```

## API para outros plugins

Interfaces em `Orion.Gameplay` (assembly Orion, já compartilhado pelo McMaster):

```csharp
public void OnEnable(IPluginContext context)
{
    if (context.Services.TryGet(out IVanillaAttributesApi? api) && api is not null)
    {
        _ = api.Health.TryHeal(player, 4f);
        _ = api.Hunger.TryAddExhaustion(player, 0.1f);
    }
}
```

Também registrados individualmente: `IEntityHealthService`, `IPlayerHungerService`, `IPlayerItemUseHandler`.

## Provides

- `orion:attributes`
- `orion:health`
- `orion:hunger`
