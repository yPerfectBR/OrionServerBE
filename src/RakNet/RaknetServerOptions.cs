using Orion.Config;

namespace Orion.RakNet;

public readonly record struct RaknetServerOptions(
    ushort MaxMtu,
    ushort MinMtu,
    int MaxConnections,
    bool EnableCookies,
    ushort PortIpv4,
    ushort PortIpv6)
{
    public static RaknetServerOptions FromOrionInfo()
    {
        if (!OrionInfo.IsLoaded)
        {
            throw new InvalidOperationException(
                "OrionInfo is not loaded. Call OrionInfo.Load() before creating RaknetServerOptions.");
        }

        RaknetConfig raknet = OrionInfo.Raknet;
        ushort minMtu = (ushort)Math.Clamp(raknet.MtuMinSize, 576, ushort.MaxValue);
        ushort maxMtu = (ushort)Math.Clamp(raknet.MtuMaxSize, minMtu, ushort.MaxValue);

        return new RaknetServerOptions(
            MaxMtu: maxMtu,
            MinMtu: minMtu,
            MaxConnections: raknet.MaxConnections,
            EnableCookies: true,
            PortIpv4: raknet.PortIPV4,
            PortIpv6: raknet.PortIPV6);
    }
}
