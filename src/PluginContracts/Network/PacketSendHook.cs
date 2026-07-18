using Orion.PluginContracts.Events;

namespace Orion.PluginContracts.Network;

public sealed class PacketSendHook
{
    public required IOrionPlugin Plugin { get; init; }

    /// <summary>Null means all packet ids (discouraged).</summary>
    public int? PacketIdFilter { get; init; }

    public EventPriority Priority { get; init; } = EventPriority.Normal;

    public required Action<PacketSendContext> Handler { get; init; }
}
