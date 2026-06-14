using Orion.Config;
using Log = Orion.Logger.Logger;

namespace Orion.Logger.Tests;

public sealed class LoggerConfigTests
{
    [Fact]
    public void LoadServerJson_EnablesConfiguredLevels()
    {
        string configPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "config", "server.json"));

        OrionInfo.Load(configPath);
        Log.Configure(OrionInfo.Logging.LogLevel);

        Assert.True(Log.IsEnabled(LogCategory.RakNet, LogLevel.Info));
        Assert.True(Log.IsEnabled(LogCategory.Protocol, LogLevel.Debug));
    }

    [Fact]
    public void DisabledLevel_IsFiltered()
    {
        var levels = new LogLevelConfig
        {
            RakNet = new CategoryLogLevel
            {
                Debug = false,
                Info = true,
                Warn = true,
                Error = true,
                Chat = true
            }
        };

        Log.Configure(levels);

        Assert.False(Log.IsEnabled(LogCategory.RakNet, LogLevel.Debug));
        Assert.True(Log.IsEnabled(LogCategory.RakNet, LogLevel.Info));
    }
}
