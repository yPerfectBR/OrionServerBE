using Orion.Config;
using Orion.PluginContracts;
using Orion.Plugins;

namespace Orion.Game.Tests;

[Collection("PluginHost")]
public sealed class PluginLoadOrderTests
{
    [Fact]
    public void Sort_HardDepend_OrdersDependencyFirst()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("orion:inventory", depend: [Dep("orion:containers")]),
            Manifest("orion:containers")
        ]);
        Assert.Equal(["orion:containers", "orion:inventory"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_MissingHardDepend_Throws()
    {
        PluginManifestException ex = Assert.Throws<PluginManifestException>(
            () => PluginLoadOrder.Sort([Manifest("orion:inventory", depend: [Dep("orion:missing")])]));
        Assert.Equal("DEPEND_MISSING", ex.ErrorCode);
        Assert.Contains("orion:missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sort_SoftDependAbsent_StillLoads()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
            [Manifest("orion:building", softDepend: [Soft("orion:inventory")])]);
        Assert.Equal(["orion:building"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_SoftDependAfter_OrdersDependencyFirst()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("orion:building", softDepend: [Soft("orion:inventory")]),
            Manifest("orion:inventory")
        ]);
        Assert.Equal(["orion:inventory", "orion:building"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_SoftDependBefore_OrdersRequesterFirst()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("orion:attributes"),
            Manifest("orion:inventory", softDepend: [Soft("orion:attributes", PluginSoftLoadOrder.Before)])
        ]);
        Assert.Equal(["orion:inventory", "orion:attributes"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_Cycle_Throws()
    {
        PluginManifestException ex = Assert.Throws<PluginManifestException>(
            () => PluginLoadOrder.Sort(
            [
                Manifest("orion:a", depend: [Dep("orion:b")]),
                Manifest("orion:b", depend: [Dep("orion:a")])
            ]));
        Assert.Equal("ORDER_CYCLE", ex.ErrorCode);
    }

    [Fact]
    public void Sort_DuplicateId_Throws()
    {
        PluginManifestException ex = Assert.Throws<PluginManifestException>(
            () => PluginLoadOrder.Sort([Manifest("orion:a"), Manifest("orion:a")]));
        Assert.Equal("MANIFEST_REGEX", ex.ErrorCode);
    }

    [Fact]
    public void Sort_VersionOutOfRange_Throws()
    {
        PluginManifestException ex = Assert.Throws<PluginManifestException>(
            () => PluginLoadOrder.Sort(
            [
                Manifest("orion:inventory", depend: [Dep("orion:containers", "1.0.0", "2.0.0")]),
                Manifest("orion:containers", version: "3.0.0")
            ]));
        Assert.Equal("VERSION_OUT_OF_RANGE", ex.ErrorCode);
    }

    [Fact]
    public void Sort_CrossConstraintConflict_Throws()
    {
        PluginManifestException ex = Assert.Throws<PluginManifestException>(
            () => PluginLoadOrder.Sort(
            [
                Manifest("orion:a", depend: [Dep("orion:core", "1.0.0", "2.0.0")]),
                Manifest("orion:b", depend: [Dep("orion:core", "3.0.0", "4.0.0")]),
                Manifest("orion:core", version: "2.5.0")
            ]));
        Assert.Equal("VERSION_CONSTRAINT_CONFLICT", ex.ErrorCode);
    }

    [Fact]
    public void Sort_AlphabeticalTieBreak_WhenNoEdges()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("orion:zed"),
            Manifest("orion:alpha"),
            Manifest("orion:mid")
        ]);
        Assert.Equal(["orion:alpha", "orion:mid", "orion:zed"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void ResolveAssemblyPath_ColonId_MapsToDotDll()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string dllPath = Path.Combine(directory, "orion.inventory.dll");
            File.WriteAllText(dllPath, string.Empty);
            string resolved = PluginManifest.ResolveAssemblyPath(directory, "orion:inventory");
            Assert.Equal(dllPath, resolved);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ParseFile_InvalidId_ThrowsManifestRegex()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string directory = Path.Combine(root, "badid");
        Directory.CreateDirectory(directory);
        string manifestPath = Path.Combine(directory, "plugin.json");
        try
        {
            File.WriteAllText(manifestPath,
                """
                {
                  "id": "badid",
                  "version": "1.0.0",
                  "api": "0.1.0",
                  "main": "X.Y",
                  "depend": []
                }
                """);

            PluginManifestException ex = Assert.Throws<PluginManifestException>(
                () => PluginManifest.ParseFile(manifestPath));
            Assert.Equal("MANIFEST_REGEX", ex.ErrorCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ParseFile_HyphenatedProductId_Succeeds()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string directory = Path.Combine(root, "orion:block-containers");
        Directory.CreateDirectory(directory);
        string manifestPath = Path.Combine(directory, "plugin.json");
        try
        {
            File.WriteAllText(manifestPath,
                """
                {
                  "id": "orion:block-containers",
                  "version": "1.0.0",
                  "api": "0.1.0",
                  "main": "X.Y",
                  "depend": []
                }
                """);
            File.WriteAllText(Path.Combine(directory, "orion.block-containers.dll"), string.Empty);

            PluginManifest manifest = PluginManifest.ParseFile(manifestPath);
            Assert.Equal("orion:block-containers", manifest.Id);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ParseFile_UnderscoreProductId_Succeeds()
    {
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string directory = Path.Combine(root, "orion:block_containers");
        Directory.CreateDirectory(directory);
        string manifestPath = Path.Combine(directory, "plugin.json");
        try
        {
            File.WriteAllText(manifestPath,
                """
                {
                  "id": "orion:block_containers",
                  "version": "1.0.0",
                  "api": "0.1.0",
                  "main": "X.Y",
                  "depend": []
                }
                """);
            File.WriteAllText(Path.Combine(directory, "orion.block_containers.dll"), string.Empty);

            PluginManifest manifest = PluginManifest.ParseFile(manifestPath);
            Assert.Equal("orion:block_containers", manifest.Id);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LoadConfigured_WhenDisabled_LoadsNothing()
    {
        PluginHost.ResetForTests();
        PluginHost.LoadConfigured(new OrionConfig
        {
            Plugins = new PluginsConfig { Enabled = false, Directory = "plugins" }
        });
        Assert.Empty(PluginHost.LoadedPluginIds);
        PluginHost.ResetForTests();
    }

    static PluginDependency Dep(string id, string min = "1.0.0", string max = "99.0.0") =>
        new()
        {
            Id = id,
            MinVersion = Version.Parse(min),
            MaxVersion = Version.Parse(max)
        };

    static PluginSoftDependency Soft(
        string id,
        PluginSoftLoadOrder load = PluginSoftLoadOrder.After,
        string min = "1.0.0",
        string max = "99.0.0") =>
        new()
        {
            Id = id,
            Load = load,
            MinVersion = Version.Parse(min),
            MaxVersion = Version.Parse(max)
        };

    static PluginManifest Manifest(
        string id,
        string version = "1.0.0",
        PluginDependency[]? depend = null,
        PluginSoftDependency[]? softDepend = null) =>
        new()
        {
            Id = id,
            Version = Version.Parse(version),
            ApiVersion = new Version(0, 1, 0),
            Main = id + ".Main",
            Depend = depend ?? [],
            SoftDepend = softDepend ?? [],
            Provides = []
        };
}
