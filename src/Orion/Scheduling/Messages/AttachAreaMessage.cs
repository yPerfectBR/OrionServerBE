using Orion.World;

namespace Orion.Scheduling.Messages;

public sealed class AttachAreaMessage : IAreaMessage
{
    public required Dimension Dimension { get; init; }

    public required int AreaIndex { get; init; }
}
