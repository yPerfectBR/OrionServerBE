using Orion.Player;
using Orion.Entity.Traits.Types;

namespace Orion.Events;

public sealed class PlayerSpawnSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerSpawn;
    public EntitySpawnOptions Options;
    public bool Cancelled;

    public PlayerSpawnSignal(Orion.Player.Player player, EntitySpawnOptions options) : base(player)
    {
        Options = options;
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }
}






