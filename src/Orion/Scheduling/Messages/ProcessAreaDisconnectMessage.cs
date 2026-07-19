using Orion.RakNet;

namespace Orion.Scheduling.Messages;

public sealed class ProcessAreaDisconnectMessage : IAreaMessage
{
    public required NetworkConnection Connection { get; init; }
}
