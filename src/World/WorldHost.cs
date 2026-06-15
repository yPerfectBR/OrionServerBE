using Orion.Config;
using Orion.World.Generation;
using Orion.World.Pregeneration;
using Orion.World.Provider;
using Orion.World.Threading;
using Orion.Protocol.Enums;
using WorldLogger = Orion.Logger.Logger;

namespace Orion.World;

/// <summary>
/// Bootstraps world settings, persistence, dimensions, pregeneration, and tick scheduling.
/// </summary>
public sealed class WorldHost : IDisposable
{
    public World World { get; }

    public IReadOnlyDictionary<string, AreaResolver> AreaResolvers { get; }

    public AreaThreadScheduler Scheduler { get; }

    /// <summary>
    /// True after all configured chunk pregeneration has finished.
    /// </summary>
    public bool IsPregenerationComplete { get; private set; }

    /// <summary>
    /// Players may join only when pregeneration is complete.
    /// </summary>
    public bool CanAcceptPlayers => IsPregenerationComplete;

    private WorldHost(
        World world,
        Dictionary<string, AreaResolver> areaResolvers,
        AreaThreadScheduler scheduler)
    {
        World = world;
        AreaResolvers = areaResolvers;
        Scheduler = scheduler;
        IsPregenerationComplete = true;
    }

    public static WorldHost Bootstrap(OrionConfig config, string? worldsRoot = null, bool startScheduler = true)
    {
        ArgumentNullException.ThrowIfNull(config);

        OrionInfo.SetCanAcceptPlayers(false);

        string spawnWorldIdentifier = config.Server.Orion.SpawnWorldIdentifier;
        ResolvedWorldSettings resolved = WorldSettingsResolver.Resolve(
            spawnWorldIdentifier,
            config.Server.WorldDefaultSettings,
            worldsRoot);

        string dbPath = Path.Combine(resolved.DirectoryPath, "db");
        Directory.CreateDirectory(resolved.DirectoryPath);

        World world = new(resolved.Identifier, new LevelDbProvider(dbPath));
        Dictionary<string, AreaResolver> areaResolvers = new(StringComparer.OrdinalIgnoreCase);
        ChunkPregenerator pregenerator = new();

        WorldLogger.Info(LogCategory.World, "Starting chunk pregeneration for world '{0}'", resolved.Identifier);

        foreach (DimensionConfig dimensionConfig in resolved.Settings.Dimensions)
        {
            Generator generator = GeneratorFactory.Create(dimensionConfig.Generator);
            Dimension dimension = world.CreateDimension(
                dimensionConfig.Identifier,
                (DimensionType)dimensionConfig.Type,
                generator,
                dimensionConfig.ThreadingAreas);

            areaResolvers[dimensionConfig.Identifier] = new AreaResolver(dimensionConfig.ThreadingAreas);
            pregenerator.PregenerateAll(dimension, dimensionConfig.ChunkPregeneration ?? [], dimensionConfig.Identifier);
        }

        WorldLogger.Info(LogCategory.World, "Chunk pregeneration complete for world '{0}'", resolved.Identifier);

        AreaThreadScheduler scheduler = new(world, config.Server.Orion.TicksPerSecond);
        if (startScheduler)
        {
            scheduler.Start();
        }

        OrionInfo.SetCanAcceptPlayers(true);

        WorldHost host = new(world, areaResolvers, scheduler);
        host.IsPregenerationComplete = true;
        return host;
    }

    public int ResolveArea(string dimensionIdentifier, int chunkX, int chunkZ)
    {
        if (!AreaResolvers.TryGetValue(dimensionIdentifier, out AreaResolver? resolver))
        {
            return AreaResolver.DefaultThread;
        }

        return resolver.ResolveArea(chunkX, chunkZ);
    }

    public void Dispose()
    {
        OrionInfo.SetCanAcceptPlayers(false);
        Scheduler.Dispose();
        World.Dispose();
    }
}
