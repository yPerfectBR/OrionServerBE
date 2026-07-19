using System.Text.Json;

namespace Orion.Config;

/// <summary>
/// Global configuration holder. Must be loaded before any other module starts.
/// </summary>
public static class OrionInfo
{
    private static readonly Lock Sync = new();
    private static OrionConfig? _config;
    private static string _configPath = "config/server.json";
    private static long _serverGuid;

    /// <summary>
    /// Optional hook for live player count in server-list pings. Returns 0 when unset or on failure.
    /// </summary>
    public static Func<int>? ActivePlayerCountProvider { get; set; }

    /// <summary>
    /// When false, new player connections must be rejected until world pregeneration completes.
    /// </summary>
    public static bool CanAcceptPlayers
    {
        get
        {
            lock (Sync)
            {
                return _canAcceptPlayers;
            }
        }
    }

    private static bool _canAcceptPlayers;

    public static long ServerGuid
    {
        get
        {
            lock (Sync)
            {
                EnsureLoaded();
                EnsureServerGuid();
                return _serverGuid;
            }
        }
    }

    public static bool IsLoaded
    {
        get
        {
            lock (Sync)
            {
                return _config is not null;
            }
        }
    }

    public static string ConfigPath
    {
        get
        {
            lock (Sync)
            {
                return _configPath;
            }
        }
    }

    public static OrionConfig Config
    {
        get
        {
            lock (Sync)
            {
                return _config ?? throw new InvalidOperationException(
                    "OrionInfo is not loaded. Call OrionInfo.Load() before accessing configuration.");
            }
        }
    }

    public static LoggingConfig Logging => Config.Logging;
    public static ServerSection Server => Config.Server;
    public static OrionSection Orion => Config.Server.Orion;
    public static NetworkConfig Network => Config.Server.Network;
    public static RaknetConfig Raknet => Config.Server.Raknet;
    public static WorldProperties WorldDefaultSettings => Config.Server.WorldDefaultSettings;
    public static RuntimeConfig Runtime => Config.Runtime;
    public static string SpawnWorldIdentifier => Config.Server.Orion.SpawnWorldIdentifier;

    /// <summary>
    /// Updates whether the server may accept new player connections.
    /// </summary>
    public static void SetCanAcceptPlayers(bool canAccept)
    {
        lock (Sync)
        {
            _canAcceptPlayers = canAccept;
        }
    }

    /// <summary>
    /// Loads configuration from disk. Safe to call once at process startup.
    /// </summary>
    public static void Load(string? configPath = null)
    {
        string path = configPath ?? _configPath;
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: {path}");
        }

        string json = File.ReadAllText(path);
        OrionConfig? config = JsonSerializer.Deserialize(json, OrionJsonContext.Default.OrionConfig);
        if (config is null)
        {
            throw new InvalidOperationException($"Failed to deserialize configuration: {path}");
        }

        OrionRuntime.Apply(config.Runtime);
        SetCanAcceptPlayers(false);

        lock (Sync)
        {
            _config = config;
            _configPath = path;
            EnsureServerGuid();
        }
    }

    /// <summary>
    /// Injects a pre-built configuration (tests or host bootstrap).
    /// </summary>
    public static void Load(OrionConfig config, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        OrionRuntime.Apply(config.Runtime);
        SetCanAcceptPlayers(false);

        lock (Sync)
        {
            _config = config;
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                _configPath = configPath;
            }

            EnsureServerGuid();
        }
    }

    /// <summary>
    /// Builds the MCPE server-list advertisement string for RakNet UnconnectedPong.
    /// Format: Edition;MOTD;Protocol;Version;PlayerCount;MaxPlayers;ServerGuid;SubMotd;Gamemode;GamemodeId;PortV4;PortV6;
    /// </summary>
    public static string BuildRaknetAdvertisement()
    {
        RaknetConfig raknet = Raknet;
        ServerSection server = Server;
        string gamemode = FormatGamemode(server.WorldDefaultSettings.Gamemode);
        int gamemodeNumeric = ResolveGamemodeNumeric(server.WorldDefaultSettings.Gamemode);
        int playerCount = ResolveActivePlayerCount();

        return string.Join(';',
        [
            server.Edition,
            server.Motd,
            raknet.Protocol.ToString(),
            raknet.Version,
            playerCount.ToString(),
            raknet.MaxConnections.ToString(),
            ServerGuid.ToString(),
            raknet.Message,
            gamemode,
            gamemodeNumeric.ToString(),
            raknet.PortIPV4.ToString(),
            raknet.PortIPV6.ToString(),
            ""
        ]);
    }

    private static void EnsureLoaded()
    {
        if (_config is null)
        {
            throw new InvalidOperationException(
                "OrionInfo is not loaded. Call OrionInfo.Load() before accessing configuration.");
        }
    }

    private static void EnsureServerGuid()
    {
        if (_serverGuid == 0)
        {
            _serverGuid = Random.Shared.NextInt64(1, long.MaxValue);
        }
    }

    private static int ResolveActivePlayerCount()
    {
        try
        {
            return Math.Max(0, ActivePlayerCountProvider?.Invoke() ?? 0);
        }
        catch
        {
            return 0;
        }
    }

    private static string FormatGamemode(string gamemode)
    {
        if (string.IsNullOrWhiteSpace(gamemode))
        {
            return "Survival";
        }

        return char.ToUpperInvariant(gamemode[0]) + gamemode[1..].ToLowerInvariant();
    }

    private static int ResolveGamemodeNumeric(string gamemode) => gamemode.ToLowerInvariant() switch
    {
        "creative" => 2,
        "adventure" => 3,
        _ => 1
    };
}
