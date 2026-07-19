namespace Orion.Events;

using Orion.Player;

public sealed class PlayerJoinSignal : PlayerSignal, ICancellable
{
    public override ServerEvent Event => ServerEvent.PlayerJoin;
    public bool Cancelled { get; private set; }

    public PlayerJoinSignal(Player player) : base(player)
    {
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }

    internal void SetCancelled(bool cancelled) => Cancelled = cancelled;
}
