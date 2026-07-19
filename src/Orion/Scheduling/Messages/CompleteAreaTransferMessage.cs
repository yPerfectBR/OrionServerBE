namespace Orion.Scheduling.Messages;

public sealed class CompleteAreaTransferMessage : IAreaMessage
{
    public required AreaEntitySnapshot Snapshot { get; init; }
}
