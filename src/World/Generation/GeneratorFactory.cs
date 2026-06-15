namespace Orion.World.Generation;

public static class GeneratorFactory
{
    public static Generator Create(string identifier) =>
        identifier.ToLowerInvariant() switch
        {
            "superflat" => new SuperFlatGenerator(),
            "void" => new VoidGenerator(),
            _ => new VoidGenerator()
        };
}
