namespace Orion.Scheduling.Messages;

public sealed class RunOnAreaThreadMessage : IAreaMessage
{
    public required Action Action { get; init; }

    public TaskCompletionSource<object?>? Completion { get; init; }
}
