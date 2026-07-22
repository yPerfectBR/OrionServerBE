using Orion.Api.Math;

namespace Orion.Api.Traits;

public readonly record struct EntityTeleportDetails(Vec3f From, Vec3f To, bool ForceFullChunkReload = false);

public sealed class EntityMovementRotation
{
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float HeadYaw { get; set; }
}

public readonly record struct EntityMoveDetails(
    Vec3f From,
    Vec3f To,
    EntityMovementRotation FromRotation,
    EntityMovementRotation ToRotation);

public readonly record struct EntityDespawnDetails(bool IsRemoval = false, bool Disconnected = false);
