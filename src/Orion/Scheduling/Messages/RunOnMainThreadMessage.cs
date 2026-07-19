namespace Orion.Scheduling.Messages;

public sealed class RunOnMainThreadMessage : INetworkQueueMessage
{
    public required Action Action { get; init; }

    public TaskCompletionSource<object?>? Completion { get; init; }
}
