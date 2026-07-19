using Orion.Config;
using Orion.Protocol.Enums;
using Orion.World.Generation;
using Orion.World.Provider;

namespace Orion.World;

public sealed class World : IDisposable, Tickable
{
    public object? Server { get; set; }
    private readonly Dictionary<string, Dimension> _dimensions = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; }

    public WorldProvider Provider { get; }

    /// <summary>
    /// World-level gamerules from resolved world settings (sent to clients on join).
    /// </summary>
    public GamerulesConfig Gamerules { get; set; } = new();

    public ulong TickValue { get; set; }

    public double TickWork { get; set; }

    public int? AttachedWorkerId { get; set; }

    public int DimensionCount => _dimensions.Count;

    public IEnumerable<Dimension> Dimensions => _dimensions.Values;

    public World(string name, WorldProvider? provider = null)
    {
        Name = name;
        Provider = provider ?? new InMemoryProvider();
    }

    public Dimension CreateDimension(
        string identifier,
        DimensionType type,
        Generator generator,
        IReadOnlyList<ThreadingAreaConfig>? threadingAreas = null)
    {
        Dimension dimension = new(identifier, type, Provider, generator, threadingAreas);
        AddDimension(dimension);
        return dimension;
    }

    public Dimension CreateDimension(string identifier, DimensionType type, Type generatorType, params object[] generatorArgs)
    {
        if (!typeof(Generator).IsAssignableFrom(generatorType))
        {
            throw new ArgumentException($"Generator type must inherit {nameof(Generator)}.", nameof(generatorType));
        }

        if (Activator.CreateInstance(generatorType, generatorArgs) is not Generator generator)
        {
            throw new InvalidOperationException($"Could not construct generator '{generatorType.FullName}'.");
        }

        return CreateDimension(identifier, type, generator);
    }

    public void AddDimension(Dimension dimension)
    {
        dimension.World = this;
        _dimensions[dimension.Identifier] = dimension;
    }

    public bool RemoveDimension(string identifier)
    {
        if (!_dimensions.Remove(identifier, out Dimension? dimension))
        {
            return false;
        }

        dimension.Dispose();
        return true;
    }

    public Dimension? GetDimension(string identifier) =>
        _dimensions.TryGetValue(identifier, out Dimension? dimension) ? dimension : null;

    public Dimension? GetDimension(DimensionType type) =>
        _dimensions.Values.FirstOrDefault(d => d.Type == type);

    public void Tick()
    {
        TickValue++;
        foreach (Dimension dimension in _dimensions.Values)
        {
            dimension.Tick(TickValue, 1);
        }
    }

    public void Dispose()
    {
        foreach (Dimension dimension in _dimensions.Values)
        {
            dimension.Dispose();
        }

        _dimensions.Clear();
        Provider.Dispose();
    }
}
