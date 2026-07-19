using Orion.Network;
using Orion.Protocol.Packets;

namespace Orion.World;

public static class DimensionBroadcastExtensions
{
    public static void Broadcast(this Dimension dimension, DataPacket packet, BroadcastOptions? options = null) =>
        BroadcastService.Broadcast(dimension, packet, options);
}
