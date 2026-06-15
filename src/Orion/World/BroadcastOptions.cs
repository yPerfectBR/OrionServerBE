using Orion.Protocol.Types;

namespace Orion.World;

public struct BroadcastOptions
{
    public float Radius = 64f;
    public Vec3f? Center;
    public global::Orion.Entity.Entity[]? Except;

    public BroadcastOptions()
    {
    }
}
