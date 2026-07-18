using Orion.Config;
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
            Manifest("A", depend: ["B"]),
            Manifest("B")
        ]);
        Assert.Equal(["B", "A"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_MissingHardDepend_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PluginLoadOrder.Sort([Manifest("A", depend: ["Missing"])]));
        Assert.Contains("Missing", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Sort_SoftDependAbsent_StillLoads()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
            [Manifest("A", softDepend: ["Missing"])]);
        Assert.Equal(["A"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_SoftDependPresent_OrdersDependencyFirst()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("A", softDepend: ["B"]),
            Manifest("B")
        ]);
        Assert.Equal(["B", "A"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_LoadBefore_OrdersRequesterFirst()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("Late"),
            Manifest("Early", loadBefore: ["Late"])
        ]);
        Assert.Equal(["Early", "Late"], ordered.Select(m => m.Id).ToArray());
    }

    [Fact]
    public void Sort_Cycle_Throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => PluginLoadOrder.Sort(
            [
                Manifest("A", depend: ["B"]),
                Manifest("B", depend: ["A"])
            ]));
        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sort_DuplicateId_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => PluginLoadOrder.Sort([Manifest("A"), Manifest("A")]));
    }

    [Fact]
    public void Sort_AlphabeticalTieBreak_WhenNoEdges()
    {
        IReadOnlyList<PluginManifest> ordered = PluginLoadOrder.Sort(
        [
            Manifest("Zed"),
            Manifest("Alpha"),
            Manifest("Mid")
        ]);
        Assert.Equal(["Alpha", "Mid", "Zed"], ordered.Select(m => m.Id).ToArray());
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

    static PluginManifest Manifest(
        string id,
        string[]? depend = null,
        string[]? softDepend = null,
        string[]? loadBefore = null) =>
        new()
        {
            Id = id,
            Version = new Version(1, 0, 0),
            ApiVersion = new Version(0, 1, 0),
            Main = id + ".Main",
            Depend = depend ?? [],
            SoftDepend = softDepend ?? [],
            LoadBefore = loadBefore ?? [],
            Provides = []
        };
}
