namespace Orion.Scheduling.Messages;

public sealed class AbortAreaTransferMessage : IAreaMessage
{
    public required AreaEntitySnapshot Snapshot { get; init; }

    public required string Reason { get; init; }
}
