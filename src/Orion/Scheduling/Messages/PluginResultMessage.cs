namespace Orion.Scheduling.Messages;

public sealed class PluginResultMessage : IAreaMessage
{
    public required Action Apply { get; init; }

    public TaskCompletionSource<object?>? Completion { get; init; }
}
