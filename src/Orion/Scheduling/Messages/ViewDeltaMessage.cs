using Orion.Player;
using Orion.Protocol.Packets;

namespace Orion.Scheduling.Messages;

public sealed class ViewDeltaMessage : ISessionMessage
{
    public required PlayerSession Session { get; init; }

    public required DataPacket Packet { get; init; }
}
