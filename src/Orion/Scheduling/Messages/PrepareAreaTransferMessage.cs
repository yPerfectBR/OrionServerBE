namespace Orion.Scheduling.Messages;

public sealed class PrepareAreaTransferMessage : IAreaMessage
{
    public required AreaEntitySnapshot Snapshot { get; init; }
}
