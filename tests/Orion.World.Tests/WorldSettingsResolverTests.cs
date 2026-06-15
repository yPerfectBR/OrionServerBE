using System.Text.Json;
using Orion.Config;

namespace Orion.World.Tests;

public sealed class WorldSettingsResolverTests
{
    [Fact]
    public void Resolve_RejectsIdentifierMismatch()
    {
        string root = CreateWorldRoot("default");
        WriteWorldJson(root, "default", settingsIdentifier: "other");

        Assert.Throws<InvalidOperationException>(() =>
            WorldSettingsResolver.Resolve("default", new WorldProperties(), root));
    }

    [Fact]
    public void Resolve_FallbackUsesSpawnFolderName()
    {
        string root = CreateWorldRoot("default");
        var defaults = new WorldProperties { Identifier = "ignored", Seed = 99 };

        ResolvedWorldSettings resolved = WorldSettingsResolver.Resolve("default", defaults, root);
        Assert.Equal("default", resolved.Settings.Identifier);
        Assert.Equal(99, resolved.Settings.Seed);
    }

    private static string CreateWorldRoot(string worldId)
    {
        string root = Path.Combine(Path.GetTempPath(), "orion-world-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, worldId));
        return root;
    }

    private static void WriteWorldJson(string root, string folderName, string settingsIdentifier)
    {
        var settings = new WorldProperties
        {
            Identifier = settingsIdentifier,
            Dimensions = [new DimensionConfig { Identifier = "overworld" }]
        };
        string json = JsonSerializer.Serialize(new WorldJsonFile { Settings = settings }, OrionJsonContext.Default.WorldJsonFile);
        File.WriteAllText(Path.Combine(root, folderName, "world.json"), json);
    }
}
