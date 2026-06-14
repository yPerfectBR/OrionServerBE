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
    public static StorageConfig Storage => Config.Storage;

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

        lock (Sync)
        {
            _config = config;
            _configPath = path;
        }
    }

    /// <summary>
    /// Injects a pre-built configuration (tests or host bootstrap).
    /// </summary>
    public static void Load(OrionConfig config, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        lock (Sync)
        {
            _config = config;
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                _configPath = configPath;
            }
        }
    }

    /// <summary>
    /// Builds the MCPE server-list advertisement string for RakNet UnconnectedPong.
    /// </summary>
    public static string BuildRaknetAdvertisement(long serverGuid)
    {
        RaknetConfig raknet = Raknet;
        ServerSection server = Server;

        return $"MCPE;{server.Motd};{raknet.Protocol};{raknet.Version};0;10;{serverGuid};{raknet.Message};Survival;1;{raknet.PortIPV4};{raknet.PortIPV6};";
    }
}
