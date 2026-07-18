namespace Orion.Events;

using Orion.Player;

public sealed class PlayerChatSignal : PlayerSignal, ICancellable
{
    public override ServerEvent Event => ServerEvent.PlayerChat;
    public string RawMessage { get; }
    public string Message;
    public bool Cancelled { get; private set; }

    public PlayerChatSignal(Player player, string rawMessage, string message) : base(player)
    {
        RawMessage = rawMessage;
        Message = message;
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
