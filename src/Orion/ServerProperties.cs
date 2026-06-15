namespace Orion;

public sealed class ServerProperties
{
    public int TicksPerSecond { get; set; } = 20;

    public int AreaThreadCount { get; set; } = 8;

    public int SessionThreadCount { get; set; } = 4;

    public bool AreaThreadingEnabled { get; set; }

    public bool SessionThreadingEnabled { get; set; }

    public bool AreaSchedulerDebug { get; set; }

    public bool WorldSchedulerDebug { get; set; }


    public int SimulationDistance { get; set; } = 10;

    public int MaxViewDistance { get; set; } = 32;

    public bool OnlineMode { get; set; } = true;


    public Orion.Protocol.Enums.CompressionMethod CompressionMethod { get; set; } =
        Orion.Protocol.Enums.CompressionMethod.Zlib;

    public int CompressionThreshold { get; set; } = 256;

    public int Mtu { get; set; } = 1492;

    public int Port { get; set; } = 19132;
}
