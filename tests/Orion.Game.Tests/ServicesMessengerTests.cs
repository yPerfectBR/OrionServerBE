using System.Text;
using Orion.PluginContracts;
using Orion.PluginContracts.Messaging;
using Orion.PluginContracts.Services;
using Orion.Plugins;
using Orion.Plugins.Messaging;
using Orion.Plugins.Services;

namespace Orion.Game.Tests;

[Collection("PluginHost")]
public sealed class ServicesMessengerTests
{
    public ServicesMessengerTests()
    {
        PluginHost.ResetForTests();
    }

    [Fact]
    public void TryGet_WithoutProvider_ReturnsFalse_GetRequiredThrows()
    {
        ServiceRegistry registry = new();
        Assert.False(registry.TryGet<ITestEconomy>(out _));
        Assert.Throws<InvalidOperationException>(() => registry.GetRequired<ITestEconomy>());
    }

    [Fact]
    public void TryGet_HigherPriorityWins()
    {
        ServiceRegistry registry = new();
        FakePlugin low = new("low");
        FakePlugin high = new("high");

        registry.Register<ITestEconomy>(new TestEconomy("low-svc"), low, ServicePriority.Low);
        registry.Register<ITestEconomy>(new TestEconomy("high-svc"), high, ServicePriority.High);

        Assert.True(registry.TryGet(out ITestEconomy? economy));
        Assert.Equal("high-svc", economy!.Name);
    }

    [Fact]
    public void TryGet_SamePriority_FirstRegisteredWins()
    {
        ServiceRegistry registry = new();
        FakePlugin a = new("a");
        FakePlugin b = new("b");

        registry.Register<ITestEconomy>(new TestEconomy("first"), a, ServicePriority.Normal);
        registry.Register<ITestEconomy>(new TestEconomy("second"), b, ServicePriority.Normal);

        Assert.True(registry.TryGet(out ITestEconomy? economy));
        Assert.Equal("first", economy!.Name);
    }

    [Fact]
    public void UnregisterAll_RemovesOwnerServices()
    {
        ServiceRegistry registry = new();
        FakePlugin owner = new("owner");
        registry.Register<ITestEconomy>(new TestEconomy("x"), owner, ServicePriority.Normal);
        registry.UnregisterAll(owner);
        Assert.False(registry.TryGet<ITestEconomy>(out _));
    }

    [Fact]
    public void DisableAll_UnregistersServicesAndMessenger()
    {
        ProviderPlugin provider = new();
        ConsumerPlugin consumer = new();

        PluginHost.RegisterLoadedForTests(
            provider,
            Manifest("provider"));
        PluginHost.RegisterLoadedForTests(
            consumer,
            Manifest("consumer", softDepend:
            [
                new PluginSoftDependency
                {
                    Id = "provider",
                    MinVersion = new Version(1, 0, 0),
                    MaxVersion = new Version(99, 0, 0)
                }
            ]));

        Server server = new();
        PluginHost.EnableAll(server);

        Assert.True(PluginHost.Services.TryGet(out ITestEconomy? economy));
        Assert.Equal("live", economy!.Name);
        Assert.True(consumer.GotEconomy);

        byte[] payload = Encoding.UTF8.GetBytes("ping");
        PluginHost.Messenger.Publish("test:channel", payload, provider);
        Assert.Equal(1, consumer.MessageCount);

        PluginHost.DisableAll();
        Assert.False(PluginHost.Services.TryGet<ITestEconomy>(out _));

        consumer.MessageCount = 0;
        PluginHost.Messenger.Publish("test:channel", payload, provider);
        Assert.Equal(0, consumer.MessageCount);
    }

    [Fact]
    public void Messenger_Publish_DeliversToAllSubscribers()
    {
        PluginMessenger messenger = new();
        int a = 0;
        int b = 0;
        Action<PluginMessage> ha = _ => a++;
        Action<PluginMessage> hb = _ => b++;

        messenger.Subscribe("eco:balance", ha);
        messenger.Subscribe("eco:balance", hb);
        messenger.Publish("eco:balance", Encoding.UTF8.GetBytes("1"));

        Assert.Equal(1, a);
        Assert.Equal(1, b);

        messenger.Unsubscribe("eco:balance", ha);
        messenger.Publish("eco:balance", Encoding.UTF8.GetBytes("2"));
        Assert.Equal(1, a);
        Assert.Equal(2, b);
    }

    [Fact]
    public void Messenger_InvalidChannel_Throws()
    {
        PluginMessenger messenger = new();
        Assert.Throws<ArgumentException>(() =>
            messenger.Subscribe("InvalidChannel", _ => { }));
        Assert.Throws<ArgumentException>(() =>
            messenger.Publish("no-colon", ReadOnlyMemory<byte>.Empty));
    }

    static PluginManifest Manifest(string id, PluginSoftDependency[]? softDepend = null) =>
        new()
        {
            Id = id,
            Version = new Version(1, 0, 0),
            ApiVersion = new Version(0, 1, 0),
            Main = id,
            SoftDepend = softDepend ?? [],
            Provides = id == "provider" ? ["test:economy"] : []
        };

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

    sealed class ProviderPlugin : IOrionPlugin
    {
        public string Id => "provider";
        public Version Version => new(1, 0, 0);

        public void Load(IPluginLoadContext context) { }

        public void OnEnable(IPluginContext context)
        {
            context.Services.Register<ITestEconomy>(new TestEconomy("live"), this, ServicePriority.Normal);
        }

        public void OnWorldInitialize(IWorldInitContext context) { }
        public void OnDisable(IPluginContext context) { }
    }

    sealed class ConsumerPlugin : IOrionPlugin
    {
        public string Id => "consumer";
        public Version Version => new(1, 0, 0);
        public bool GotEconomy { get; private set; }
        public int MessageCount { get; set; }

        public void Load(IPluginLoadContext context) { }

        public void OnEnable(IPluginContext context)
        {
            GotEconomy = context.Services.TryGet<ITestEconomy>(out _);
            context.Messenger.Subscribe("test:channel", _ => MessageCount++);
        }

        public void OnWorldInitialize(IWorldInitContext context) { }
        public void OnDisable(IPluginContext context) { }
    }
}
