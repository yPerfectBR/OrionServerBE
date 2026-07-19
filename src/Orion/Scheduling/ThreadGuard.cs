using System.Diagnostics;
using Orion.World;
using Orion.World.Threading;
using WorldInstance = Orion.World.World;

namespace Orion.Scheduling;

#if DEBUG
public static class ThreadGuard
{
    [ThreadStatic]
    public static int? CurrentWorkerId;

    [ThreadStatic]
    public static int? CurrentAreaWorkerId;

    [ThreadStatic]
    public static int? CurrentAreaIndex;

    public static void AssertWorldThread(WorldInstance world)
    {
        System.Diagnostics.Debug.Assert(world.AttachedWorkerId == CurrentWorkerId);
    }

    public static void AssertAreaThread(Dimension dimension, int areaIndex)
    {
        AreaShard shard = dimension.GetAreaShard(areaIndex);
        if (!shard.IsAttached)
        {
            return;
        }

        System.Diagnostics.Debug.Assert(shard.AttachedWorkerId == CurrentAreaWorkerId);
        System.Diagnostics.Debug.Assert(!CurrentAreaIndex.HasValue || CurrentAreaIndex == areaIndex);
    }

    public static void AssertSimulationThread(Dimension dimension, WorldInstance world)
    {
        if (CurrentAreaWorkerId.HasValue)
        {
            if (CurrentAreaIndex.HasValue)
            {
                AssertAreaThread(dimension, CurrentAreaIndex.Value);
            }

            return;
        }

        AssertWorldThread(world);
    }
}
#endif
