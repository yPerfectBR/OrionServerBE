using Orion.Protocol.Enums;
using Orion.RakNet;

namespace Orion.Scheduling.Messages;

public sealed class ProcessPacketMessage : INetworkQueueMessage
{
    public required NetworkConnection Connection { get; init; }

    public required PacketId PacketId { get; init; }

    public required byte[] Payload { get; init; }
}
