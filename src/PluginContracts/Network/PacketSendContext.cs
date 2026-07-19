using Orion.PluginContracts.Events;

namespace Orion.PluginContracts.Network;

public sealed class PacketSendContext : ICancellable
{
    public required IPlayerConnection Connection { get; init; }

    public required int PacketId { get; init; }

    public required ReadOnlyMemory<byte> Payload { get; init; }

    public bool Cancelled { get; private set; }

    public void Cancel() => Cancelled = true;

    /// <summary>Optional replacement body (same PacketId).</summary>
    public byte[]? ReplacementPayload { get; set; }

    internal void SetCancelled(bool cancelled) => Cancelled = cancelled;
}
