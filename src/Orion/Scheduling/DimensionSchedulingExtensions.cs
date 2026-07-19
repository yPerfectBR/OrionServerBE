using Orion.World;
using Orion.World.Coordinates;
using Orion.World.Threading;
using GameEntity = global::Orion.Entity.Entity;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.Scheduling;

public static class DimensionSchedulingExtensions
{
    public static bool UsesAreaThreading(this Dimension dimension) =>
        dimension.ShardManager.ShardCount > 1;

    public static bool UsesRegionThreading(this Dimension dimension) =>
        dimension.UsesAreaThreading();

    public static int ResolveAreaIndex(this Dimension dimension, float blockX, float blockZ)
    {
        ChunkCoord chunk = ChunkCoord.FromBlock(blockX, blockZ);
        return dimension.ShardManager.Resolver.ResolveArea(chunk.X, chunk.Z);
    }

    public static AreaShard GetAreaShard(this Dimension dimension, int areaIndex) =>
        dimension.ShardManager.GetShard(areaIndex);

    public static void CommitCompletedChunk(this Dimension dimension, long hash, ChunkColumn? chunk)
    {
        if (chunk is null)
        {
            return;
        }

        dimension.ShardManager.ResolveShard(chunk.X, chunk.Z).SetChunk(chunk);
    }

    public static void AddEntity(this Dimension dimension, GameEntity entity)
    {
        int areaIndex = dimension.ResolveAreaIndex(entity.Position.X, entity.Position.Z);
        entity.OwningAreaIndex = areaIndex;
        dimension.ShardManager.GetShard(areaIndex).AddEntity(entity);
    }

    public static void RemoveEntity(this Dimension dimension, GameEntity entity, bool complete = true)
    {
        if (entity.OwningAreaIndex is int areaIndex)
        {
            dimension.ShardManager.GetShard(areaIndex).RemoveEntity(entity);
        }
        else
        {
            foreach (AreaShard shard in dimension.ShardManager.Shards)
            {
                shard.RemoveEntity(entity);
            }
        }

        entity.OwningAreaIndex = null;
        if (complete)
        {
            entity.CompleteDespawn();
        }
    }

    public static void AddEntity(this Dimension dimension, IAreaStoredEntity entity, int areaIndex)
    {
        if (entity is GameEntity gameplayEntity)
        {
            gameplayEntity.OwningAreaIndex = areaIndex;
        }

        dimension.ShardManager.GetShard(areaIndex).AddEntity(entity);
    }

    public static void RemoveEntity(this Dimension dimension, IAreaStoredEntity entity, int areaIndex) =>
        dimension.ShardManager.GetShard(areaIndex).RemoveEntity(entity);
}
