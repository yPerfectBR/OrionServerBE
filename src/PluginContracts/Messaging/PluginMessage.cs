namespace Orion.PluginContracts.Messaging;

public sealed class PluginMessage
{
    public required string Channel { get; init; }

    public required ReadOnlyMemory<byte> Payload { get; init; }

    public string? SenderPluginId { get; init; }
}
