using Orion.RakNet;

namespace Orion.Scheduling.Messages;

public sealed class ProcessDisconnectMessage : INetworkQueueMessage
{
    public required NetworkConnection Connection { get; init; }
}
