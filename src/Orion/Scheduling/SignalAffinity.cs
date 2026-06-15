namespace Orion.Scheduling;

using Orion.Events;
using Orion.World.Threading;
using WorldInstance = Orion.World.World;

internal static class SignalAffinity
{
    public static bool IsGlobalEvent(ISignal signal) =>
        signal.Event is ServerEvent.ServerStart or ServerEvent.PlayerJoin;

    public static WorldInstance? TryResolveWorld(Server server, ISignal signal)
    {
        switch (signal)
        {
            case PlayerSignal playerSignal:
                if (playerSignal.Player.Dimension?.World is WorldInstance playerWorld)
                {
                    return playerWorld;
                }

                return playerSignal.Event == ServerEvent.PlayerSpawn ? server.GetWorld() : null;
            case EntityHurtSignal hurtSignal:
                return hurtSignal.Entity.Dimension?.World as WorldInstance;
            case EntitySpawnSignal spawnSignal:
                return spawnSignal.Entity.Dimension?.World as WorldInstance;
            case EntityDieSignal dieSignal:
                return dieSignal.Entity.Dimension?.World as WorldInstance;
            default:
                return null;
        }
    }

    public static AreaHandle? TryResolveArea(Server server, ISignal signal)
    {
        if (!server.Properties.AreaThreadingEnabled || !server.AreaScheduler.IsActive)
        {
            return null;
        }

        return signal switch
        {
            PlayerSignal playerSignal when TryResolveAreaForEntity(playerSignal.Player, out AreaHandle playerArea) => playerArea,
            EntityHurtSignal hurtSignal when TryResolveAreaForEntity(hurtSignal.Entity, out AreaHandle hurtArea) => hurtArea,
            EntitySpawnSignal spawnSignal when TryResolveAreaForEntity(spawnSignal.Entity, out AreaHandle spawnArea) => spawnArea,
            EntityDieSignal dieSignal when TryResolveAreaForEntity(dieSignal.Entity, out AreaHandle dieArea) => dieArea,
            _ => null
        };
    }

    static bool TryResolveAreaForEntity(global::Orion.Entity.Entity entity, out AreaHandle area)
    {
        area = default;
        if (entity.Dimension is not Dimension dimension)
        {
            return false;
        }

        int chunkX = (int)entity.Position.X >> 4;
        int chunkZ = (int)entity.Position.Z >> 4;
        AreaShard shard = dimension.ShardManager.ResolveShard(chunkX, chunkZ);
        area = new AreaHandle(dimension, shard.AreaIndex);
        return true;
    }
}
