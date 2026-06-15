namespace Orion.Events;

using Orion.Player;
using Orion.Entity.Traits.Types;

public sealed class PlayerLeaveSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerLeave;
    public EntityDespawnOptions Options { get; }

    public PlayerLeaveSignal(Player player, EntityDespawnOptions options) : base(player)
    {
        Options = options;
    }
}






