namespace Orion.Scheduling;

using Orion.Events;
using Orion.Player;
using Orion.Entity.Traits.Types;
using Orion.World;

public static class AreaPlayerSpawnPipeline
{
    public static void ContinueSpawn(Server server, Player player)
    {
        _ = server;
        _ = player;
    }

    public static bool TryCompleteSpawn(
        Server server,
        Player player,
        Dimension dimension,
        out EntitySpawnOptions options)
    {
#if DEBUG
        if (dimension.UsesAreaThreading() && dimension.World is not null)
        {
            ThreadGuard.AssertSimulationThread(dimension, dimension.World);
        }
#endif

        options = new EntitySpawnOptions(InitialSpawn: true);

        if (dimension.World is null)
        {
            return false;
        }

        WorldPlayerPresence.OnPlayerEnteredWorld(server, dimension.World);

        PlayerSpawnSignal spawnSignal = new(player, options);
        server.Emit(spawnSignal);
        if (!spawnSignal.Emit())
        {
            return false;
        }

        options = spawnSignal.Options;
        player.Spawn(dimension, options);
        return true;
    }
}
