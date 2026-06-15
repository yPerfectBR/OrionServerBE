namespace Orion.Events;

using Orion.Player;

public abstract class PlayerSignal : EntitySignal
{
    public Player Player { get; }

    protected PlayerSignal(Player player)
    {
        Player = player;
    }
}






