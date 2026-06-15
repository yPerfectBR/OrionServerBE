using Orion;
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

    using ServerHost host = ServerHost.Bootstrap(OrionInfo.Config, worldsRoot);
    OrionInfo.ActivePlayerCountProvider = () => host.Server.Sessions.Count;

    NetworkServer network = new();
    host.AttachNetwork(network);

    using CancellationTokenSource shutdown = new();
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        shutdown.Cancel();
    };

    Task networkTask = Task.Run(() => network.Start(shutdown.Token), shutdown.Token);

    RaknetConfig raknet = OrionInfo.Raknet;
    ServerSection server = OrionInfo.Server;
    WorldLogger.Info(
        LogCategory.System,
        "Listening on {0}:{1} ({2}) — world '{3}'",
        raknet.Address,
        raknet.PortIPV4,
        server.Name,
        OrionInfo.SpawnWorldIdentifier);
    WorldLogger.Info(LogCategory.System, "Press Ctrl+C to stop.");

    while (!shutdown.IsCancellationRequested)
    {
        network.Tick();
        host.Scheduling.DrainNetworkQueue();
        Thread.Sleep(1);
    }

    WorldLogger.Info(LogCategory.System, "Shutting down...");
    network.Stop();

    try
    {
        await networkTask.ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
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
