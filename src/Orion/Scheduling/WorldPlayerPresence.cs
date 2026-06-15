namespace Orion.Scheduling;

using WorldInstance = Orion.World.World;

public static class WorldPlayerPresence
{
    public static void OnPlayerLeftWorld(Server server, WorldInstance world) { _ = server; _ = world; }
    public static void OnPlayerEnteredWorld(Server server, WorldInstance world) { _ = server; _ = world; }
}
