using Orion.Commands;
using Orion.Commands.List.Operator;
using Orion.Config;
using Orion.PluginContracts;
using Orion.PluginContracts.Diagnostics;
using Orion.PluginContracts.Services;
using Orion.Plugins;
using Orion.Protocol.Registry;

namespace Orion.Game.Tests;

[Collection("PluginHost")]
public sealed class PluginConflictsTests
{
    public PluginConflictsTests()
    {
        PluginHost.ResetForTests();
        CuratedItemCatalog.ResetForTests();
    }

    [Fact]
    public void RegistryDuplicate_Warn_RecordsConflict_NoThrow()
    {
        PluginHost.SetConflictModeForTests(ConflictMode.Warn);

        PluginHost.Registries.ForPlugin("owner").CreativeTabs.AddEntry("owner", 1, "minecraft:cobblestone");
        PluginHost.Registries.ForPlugin("other").CreativeTabs.AddEntry("other", 1, "minecraft:cobblestone");

        IReadOnlyList<PluginConflict> conflicts = PluginHost.Diagnostics.Conflicts;
        Assert.Single(conflicts);
        Assert.Equal("registry.item", conflicts[0].Kind);
        Assert.Equal("minecraft:cobblestone", conflicts[0].Key);
        Assert.Equal("owner", conflicts[0].WinnerPluginId);
        Assert.Equal("other", conflicts[0].LoserPluginId);

        _ = CuratedItemCatalog.GetCreativeContentPayload();
        Assert.Contains("owner", CuratedItemCatalog.GetLoadedCreativePlugins());
        Assert.DoesNotContain("other", CuratedItemCatalog.GetLoadedCreativePlugins());
    }

    [Fact]
    public void RegistryDuplicate_Fail_Throws()
    {
        PluginHost.SetConflictModeForTests(ConflictMode.Fail);

        PluginHost.Registries.ForPlugin("owner").CreativeTabs.AddEntry("owner", 1, "minecraft:cobblestone");
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            PluginHost.Registries.ForPlugin("other").CreativeTabs.AddEntry("other", 1, "minecraft:cobblestone"));

        Assert.Contains("registry.item", ex.Message);
        Assert.Single(PluginHost.Diagnostics.Conflicts);
    }

    [Fact]
    public void TwoServices_BothRegistered_ConflictListed_HighestPriorityWins()
    {
        FakePlugin low = new("low");
        FakePlugin high = new("high");

        PluginHost.Services.Register<ITestEconomy>(new TestEconomy("low-svc"), low, ServicePriority.Low);
        PluginHost.Services.Register<ITestEconomy>(new TestEconomy("high-svc"), high, ServicePriority.High);

        Assert.True(PluginHost.Services.TryGet(out ITestEconomy? economy));
        Assert.Equal("high-svc", economy!.Name);

        IReadOnlyList<PluginConflict> conflicts = PluginHost.Diagnostics.Conflicts;
        Assert.Single(conflicts);
        Assert.Equal("service", conflicts[0].Kind);
        Assert.Equal("ITestEconomy", conflicts[0].Key);
        Assert.Equal("low", conflicts[0].WinnerPluginId);
        Assert.Equal("high", conflicts[0].LoserPluginId);
    }

    [Fact]
    public void PacketSecondOwner_Warn_FalseAndConflict()
    {
        PluginHost.SetConflictModeForTests(ConflictMode.Warn);
        FakePlugin first = new("owner");
        FakePlugin second = new("other");

        Assert.True(PluginHost.Packets.TryOwnHandler(7, first, _ => { }));
        Assert.False(PluginHost.Packets.TryOwnHandler(7, second, _ => { }));

        IReadOnlyList<PluginConflict> conflicts = PluginHost.Diagnostics.Conflicts;
        Assert.Single(conflicts);
        Assert.Equal("packet.owner", conflicts[0].Kind);
        Assert.Equal("7", conflicts[0].Key);
        Assert.Equal("owner", conflicts[0].WinnerPluginId);
        Assert.Equal("other", conflicts[0].LoserPluginId);
    }

    [Fact]
    public void PacketSecondOwner_Fail_Throws()
    {
        PluginHost.SetConflictModeForTests(ConflictMode.Fail);
        FakePlugin first = new("owner");
        FakePlugin second = new("other");

        Assert.True(PluginHost.Packets.TryOwnHandler(7, first, _ => { }));
        Assert.Throws<InvalidOperationException>(() =>
            PluginHost.Packets.TryOwnHandler(7, second, _ => { }));

        Assert.Single(PluginHost.Diagnostics.Conflicts);
    }

    [Fact]
    public void PluginsCommand_IncludesConflictsSection()
    {
        PluginHost.RegisterLoadedForTests(
            new FakePlugin("demo"),
            new PluginManifest
            {
                Id = "demo",
                Version = new Version(1, 0, 0),
                Main = "demo",
                SoftDepend =
                [
                    new PluginSoftDependency
                    {
                        Id = "optional:mod",
                        MinVersion = new Version(1, 0, 0),
                        MaxVersion = new Version(99, 0, 0)
                    }
                ],
                Provides = ["demo:api"]
            });

        PluginHost.Registries.ForPlugin("owner").CreativeTabs.AddEntry("owner", 1, "minecraft:stick");
        PluginHost.Registries.ForPlugin("other").CreativeTabs.AddEntry("other", 1, "minecraft:stick");

        CommandResult result = new PluginsCommand().Execute(new CommandExecutionState
        {
            Command = "plugins",
            Executor = new ServerExecutor(),
            Server = new Server()
        });
        Assert.True(result.Success);
        string message = string.Join('\n', result.Messages);
        Assert.Contains("Conflicts", message);
        Assert.Contains("WARN", message);
        Assert.Contains("registry.item", message);
        Assert.Contains("softdepend: optional:mod", message);
        Assert.Contains("provides: demo:api", message);
    }

    interface ITestEconomy
    {
        string Name { get; }
    }

    sealed class TestEconomy(string name) : ITestEconomy
    {
        public string Name { get; } = name;
    }

    sealed class FakePlugin(string id) : IOrionPlugin
    {
        public string Id { get; } = id;
        public Version Version => new(1, 0, 0);
        public void Load(IPluginLoadContext context) { }
        public void OnEnable(IPluginContext context) { }
        public void OnWorldInitialize(IWorldInitContext context) { }
        public void OnDisable(IPluginContext context) { }
    }
}
