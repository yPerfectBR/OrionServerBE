using Orion.Player;
using Orion.Protocol.Types;
using Orion.World;

namespace Orion.Scheduling;

public sealed class AreaEntitySnapshot
{
    public required object Entity { get; init; }

    public PlayerSession? Session { get; init; }

    public required Dimension Dimension { get; init; }

    public required int SourceAreaIndex { get; init; }

    public required int TargetAreaIndex { get; init; }

    public required Vec3f Position { get; init; }

    public float Pitch { get; init; }

    public float Yaw { get; init; }

    public float HeadYaw { get; init; }

    public bool CrossWorker { get; init; }

    public static AreaEntitySnapshot Capture(
        object entity,
        PlayerSession? session,
        Dimension dimension,
        int sourceAreaIndex,
        int targetAreaIndex,
        bool crossWorker,
        Vec3f position,
        float pitch = 0f,
        float yaw = 0f,
        float headYaw = 0f) =>
        new()
        {
            Entity = entity,
            Session = session,
            Dimension = dimension,
            SourceAreaIndex = sourceAreaIndex,
            TargetAreaIndex = targetAreaIndex,
            Position = position,
            Pitch = pitch,
            Yaw = yaw,
            HeadYaw = headYaw,
            CrossWorker = crossWorker
        };
}
