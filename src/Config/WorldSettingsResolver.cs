using System.Text.Json;

namespace Orion.Config;

public static class WorldSettingsResolver
{
    public static ResolvedWorldSettings Resolve(
        string spawnWorldIdentifier,
        WorldProperties worldDefaultSettings,
        string? worldsRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spawnWorldIdentifier);
        ArgumentNullException.ThrowIfNull(worldDefaultSettings);

        worldsRoot ??= Path.Combine(Directory.GetCurrentDirectory(), "worlds");
        string worldDirectory = Path.Combine(worldsRoot, spawnWorldIdentifier);
        string worldJsonPath = Path.Combine(worldDirectory, "world.json");

        WorldProperties settings;
        if (File.Exists(worldJsonPath))
        {
            string json = File.ReadAllText(worldJsonPath);
            WorldJsonFile? file = JsonSerializer.Deserialize(json, OrionJsonContext.Default.WorldJsonFile);
            if (file?.Settings is null)
            {
                throw new InvalidOperationException($"Failed to deserialize world settings: {worldJsonPath}");
            }

            settings = file.Settings;
        }
        else
        {
            settings = CloneDefaults(worldDefaultSettings, spawnWorldIdentifier);
        }

        Validate(spawnWorldIdentifier, worldDirectory, settings);
        return new ResolvedWorldSettings(spawnWorldIdentifier, worldDirectory, settings);
    }

    private static WorldProperties CloneDefaults(WorldProperties defaults, string identifier)
    {
        return new WorldProperties
        {
            Identifier = identifier,
            Seed = defaults.Seed,
            Gamemode = defaults.Gamemode,
            Difficulty = defaults.Difficulty,
            SaveInterval = defaults.SaveInterval,
            Dimensions = defaults.Dimensions,
            Gamerules = defaults.Gamerules
        };
    }

    private static void Validate(string spawnWorldIdentifier, string worldDirectory, WorldProperties settings)
    {
        if (!string.Equals(settings.Identifier, spawnWorldIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"World identifier '{settings.Identifier}' does not match SpawnWorldIdentifier '{spawnWorldIdentifier}'.");
        }

        string folderName = Path.GetFileName(worldDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.Equals(folderName, spawnWorldIdentifier, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"World folder '{folderName}' does not match SpawnWorldIdentifier '{spawnWorldIdentifier}'.");
        }

        foreach (DimensionConfig dimension in settings.Dimensions)
        {
            ValidateChunkRegions(dimension.ChunkPregeneration ?? [], $"dimension '{dimension.Identifier}' chunkPregeneration");
            ValidateThreadingAreas(dimension.ThreadingAreas ?? [], $"dimension '{dimension.Identifier}' threadingAreas");
        }
    }

    private static void ValidateChunkRegions(IReadOnlyList<ChunkPregenerationConfig> regions, string context)
    {
        for (int i = 0; i < regions.Count; i++)
        {
            ValidateBounds(regions[i].Start, regions[i].End, context, i);
        }
    }

    private static void ValidateThreadingAreas(IReadOnlyList<ThreadingAreaConfig> areas, string context)
    {
        for (int i = 0; i < areas.Count; i++)
        {
            ValidateBounds(areas[i].Start, areas[i].End, context, i);
        }

        for (int i = 0; i < areas.Count; i++)
        {
            for (int j = i + 1; j < areas.Count; j++)
            {
                if (RegionsOverlap(areas[i], areas[j]))
                {
                    throw new InvalidOperationException(
                        $"{context}: threading areas '{areas[i].Name}' and '{areas[j].Name}' overlap.");
                }
            }
        }
    }

    private static void ValidateBounds(int[]? start, int[]? end, string context, int index)
    {
        if (start is null || end is null || start.Length < 2 || end.Length < 2)
        {
            throw new InvalidOperationException($"{context}[{index}] requires start/end with 2 chunk coordinates.");
        }

        if (start[0] > end[0] || start[1] > end[1])
        {
            throw new InvalidOperationException(
                $"{context}[{index}] has invalid bounds: start [{start[0]}, {start[1]}] must be <= end [{end[0]}, {end[1]}].");
        }
    }

    private static bool RegionsOverlap(ThreadingAreaConfig a, ThreadingAreaConfig b)
    {
        return a.Start[0] <= b.End[0] && b.Start[0] <= a.End[0]
            && a.Start[1] <= b.End[1] && b.Start[1] <= a.End[1];
    }
}

public sealed record ResolvedWorldSettings(
    string Identifier,
    string DirectoryPath,
    WorldProperties Settings);
