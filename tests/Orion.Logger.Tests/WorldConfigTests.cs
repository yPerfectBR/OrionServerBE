using System.Text.Json;
using Orion.Config;

namespace Orion.Logger.Tests;

public sealed class WorldConfigTests
{
    [Fact]
    public void DeserializeServerJson_ChunkPregenerationAsObject()
    {
        const string json = """
            {
              "dimensions": [{
                "identifier": "overworld",
                "chunkPregeneration": {
                  "start": [-10, -10],
                  "end": [10, 10],
                  "memoryLock": false
                }
              }]
            }
            """;

        WorldProperties? props = JsonSerializer.Deserialize(json, OrionJsonContext.Default.WorldProperties);
        Assert.NotNull(props);
        Assert.Single(props!.Dimensions);
        Assert.Single(props.Dimensions[0].ChunkPregeneration);
        Assert.False(props.Dimensions[0].ChunkPregeneration[0].MemoryLock);
    }

    [Fact]
    public void DeserializeServerJson_ChunkPregenerationAsArray()
    {
        const string json = """
            {
              "dimensions": [{
                "identifier": "overworld",
                "chunkPregeneration": [
                  { "start": [0, 0], "end": [1, 1], "memoryLock": true },
                  { "start": [2, 2], "end": [3, 3], "memoryLock": false }
                ]
              }]
            }
            """;

        WorldProperties? props = JsonSerializer.Deserialize(json, OrionJsonContext.Default.WorldProperties);
        Assert.NotNull(props);
        Assert.Equal(2, props!.Dimensions[0].ChunkPregeneration.Count);
    }

    [Fact]
    public void Resolve_UsesWorldJsonWhenPresent()
    {
        string root = Path.Combine(Path.GetTempPath(), "orion-world-tests", Guid.NewGuid().ToString("N"));
        string worldDir = Path.Combine(root, "default");
        Directory.CreateDirectory(worldDir);
        const string worldJson = """
            {
              "settings": {
                "identifier": "default",
                "dimensions": [{
                  "identifier": "overworld",
                  "threadingAreas": [{ "name": "default", "start": [-50, -50], "end": [50, 50] }]
                }]
              }
            }
            """;
        File.WriteAllText(Path.Combine(worldDir, "world.json"), worldJson);

        var defaults = new WorldProperties { Identifier = "other" };
        ResolvedWorldSettings resolved = WorldSettingsResolver.Resolve("default", defaults, root);

        Assert.Equal("default", resolved.Identifier);
        Assert.Equal(worldDir, resolved.DirectoryPath);
        Assert.Equal("default", resolved.Settings.Identifier);
        Assert.Single(resolved.Settings.Dimensions[0].ThreadingAreas);
    }

    [Fact]
    public void Resolve_FallbackWhenWorldJsonMissing()
    {
        string root = Path.Combine(Path.GetTempPath(), "orion-world-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "default"));

        var defaults = new WorldProperties
        {
            Identifier = "ignored",
            Seed = 42
        };

        ResolvedWorldSettings resolved = WorldSettingsResolver.Resolve("default", defaults, root);
        Assert.Equal("default", resolved.Settings.Identifier);
        Assert.Equal(42, resolved.Settings.Seed);
    }

    [Fact]
    public void Resolve_RejectsOverlappingThreadingAreas()
    {
        var settings = new WorldProperties
        {
            Identifier = "default",
            Dimensions =
            [
                new DimensionConfig
                {
                    ThreadingAreas =
                    [
                        new ThreadingAreaConfig { Name = "a", Start = [0, 0], End = [10, 10] },
                        new ThreadingAreaConfig { Name = "b", Start = [5, 5], End = [15, 15] }
                    ]
                }
            ]
        };

        string root = Path.Combine(Path.GetTempPath(), "orion-world-tests", Guid.NewGuid().ToString("N"));
        string worldDir = Path.Combine(root, "default");
        Directory.CreateDirectory(worldDir);
        string json = JsonSerializer.Serialize(new WorldJsonFile { Settings = settings }, OrionJsonContext.Default.WorldJsonFile);
        File.WriteAllText(Path.Combine(worldDir, "world.json"), json);

        Assert.Throws<InvalidOperationException>(() =>
            WorldSettingsResolver.Resolve("default", new WorldProperties(), root));
    }
}
