# VanillaAttributes

Plugin opt-in de vitais vanilla: **vida**, **fome** e consumo de comida.

`softdepend: ["VanillaInventory"]` — comer comida usa `IPlayerInventoryService` quando o inventário está carregado.

## Build

```bash
dotnet build plugins/VanillaAttributes/VanillaAttributes.csproj
```

## API

```csharp
if (context.Services.TryGet(out IVanillaAttributesApi? api) && api is not null)
{
    _ = api.Health.TryHeal(player, 4f);
    _ = api.Hunger.TryAddExhaustion(player, 0.1f);
}
```

## Provides

- `orion:attributes`
- `orion:health`
- `orion:hunger`
