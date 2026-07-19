using System.Runtime.InteropServices;
using Orion;
using Orion.Commands;
using Orion.Config;
using Orion.RakNet;
using WorldLogger = Orion.Logger.Logger;

Thread.CurrentThread.Name = "server-main";

string configPath = ResolveConfigPath(args);
string worldsRoot = ResolveWorldsRoot();

try
{
    OrionInfo.Load(configPath);
    WorldLogger.Init();

    WorldLogger.Info(LogCategory.System, "OrionServer starting...");
    WorldLogger.Info(LogCategory.System, "Config: {0}", Path.GetFullPath(configPath));
    WorldLogger.Info(LogCategory.System, "Worlds: {0}", Path.GetFullPath(worldsRoot));

    foreach (string warning in OrionRuntime.ValidateThreadPool(OrionInfo.Config))
    {
        WorldLogger.Warn(LogCategory.System, warning);
    }

    Orion.Plugins.PluginHost.LoadConfigured(OrionInfo.Config);
    Orion.Item.ItemRegistry.EnsureLoaded();
    Orion.Plugins.PluginHost.NotifyCatalogLoaded();

    using ServerHost host = ServerHost.Bootstrap(OrionInfo.Config, worldsRoot);
    Orion.Plugins.PluginHost.NotifyWorldBootstrapped();
    Orion.Plugins.PluginHost.EnableAll(host.Server);
    Orion.Plugins.PluginHost.InitializeWorld();
    OrionInfo.ActivePlayerCountProvider = () => host.Server.Sessions.Count;

    NetworkServer network = new();
    host.AttachNetwork(network);

    using CancellationTokenSource shutdown = new();
    RegisterShutdownSignals(shutdown);

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        shutdown.Cancel();
    };

    Thread consoleThread = new(() => ConsoleInterface.Run(host.Server, shutdown.Token, shutdown.Cancel))
    {
        IsBackground = true,
        Name = "server-console"
    };
    consoleThread.Start();

    Thread networkThread = new(() => RunNetworkLoop(network, shutdown.Token))
    {
        IsBackground = true,
        Name = "raknet-udp"
    };
    networkThread.Start();

    for (int i = 0; i < 50 && network.LocalEndPoint is null && !shutdown.IsCancellationRequested; i++)
    {
        Thread.Sleep(20);
    }

    if (network.LocalEndPoint is null)
    {
        throw new InvalidOperationException("UDP socket failed to bind — check port 19132 is free.");
    }

    RaknetConfig raknet = OrionInfo.Raknet;
    ServerSection server = OrionInfo.Server;
    WorldLogger.Info(
        LogCategory.System,
        "Listening on {0} ({1}) — world '{2}'",
        network.LocalEndPoint,
        server.Name,
        OrionInfo.SpawnWorldIdentifier);
    WorldLogger.Info(LogCategory.System, "Add server manually: <host-ip>:{0} (client must match protocol {1})",
        raknet.PortIPV4,
        raknet.Protocol);
    WorldLogger.Info(LogCategory.System, "Type commands in the console, 'stop', or press Ctrl+C to shut down.");

    while (!shutdown.IsCancellationRequested)
    {
        network.Tick();
        host.Scheduling.DrainNetworkQueue();
        Thread.Sleep(1);
    }

    WorldLogger.Info(LogCategory.System, "Shutting down...");
    Orion.Plugins.PluginHost.DisableAll();
    network.Stop();
    host.Scheduling.Stop();

    if (!networkThread.Join(TimeSpan.FromSeconds(3)))
    {
        WorldLogger.Warn(LogCategory.System, "Network thread did not exit in time.");
    }

    WorldLogger.Info(LogCategory.System, "OrionServer stopped.");
    return 0;
}
catch (Exception exception)
{
    try
    {
        WorldLogger.Error(LogCategory.System, "Fatal: {0}", exception.Message);
    }
    catch
    {
        Console.Error.WriteLine($"Fatal: {exception}");
    }

    return 1;
}

static void RegisterShutdownSignals(CancellationTokenSource shutdown)
{
    if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
    {
        return;
    }

    PosixSignalRegistration.Create(PosixSignal.SIGINT, _ => shutdown.Cancel());
    PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => shutdown.Cancel());
}

static void RunNetworkLoop(NetworkServer network, CancellationToken cancellationToken)
{
    try
    {
        network.Start(cancellationToken).AsTask().GetAwaiter().GetResult();
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
    catch (Exception exception)
    {
        WorldLogger.Error(LogCategory.System, "Network loop failed: {0}", exception);
    }
}

    static string ResolveConfigPath(string[] args)
{
    string? fromEnv = Environment.GetEnvironmentVariable("ORION_CONFIG_PATH");
    if (!string.IsNullOrWhiteSpace(fromEnv))
    {
        return fromEnv;
    }

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] is "--config" or "-c" && i + 1 < args.Length)
        {
            return args[i + 1];
        }

        if (args[i].EndsWith("server.json", StringComparison.OrdinalIgnoreCase))
        {
            return args[i];
        }
    }

    return Path.Combine("config", "server.json");
}

static string ResolveWorldsRoot()
{
    string? fromEnv = Environment.GetEnvironmentVariable("ORION_WORLDS_PATH");
    return string.IsNullOrWhiteSpace(fromEnv) ? "worlds" : fromEnv;
}
