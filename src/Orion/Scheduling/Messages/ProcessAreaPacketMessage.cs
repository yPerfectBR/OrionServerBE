using Orion.Protocol.Enums;
using Orion.RakNet;
using Orion.World;

namespace Orion.Scheduling.Messages;

public sealed class ProcessAreaPacketMessage : IAreaMessage
{
    public required NetworkConnection Connection { get; init; }

    public required PacketId PacketId { get; init; }

    public required byte[] Payload { get; init; }

    public required Dimension Dimension { get; init; }

    public required int AreaIndex { get; init; }
}
