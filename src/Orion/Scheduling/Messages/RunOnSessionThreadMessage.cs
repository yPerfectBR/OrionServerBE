namespace Orion.Scheduling.Messages;

public sealed class RunOnSessionThreadMessage : ISessionMessage
{
    public required Action Action { get; init; }

    public TaskCompletionSource<object?>? Completion { get; init; }
}
