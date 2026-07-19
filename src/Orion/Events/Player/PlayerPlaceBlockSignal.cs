namespace Orion.Events;

using Orion.Player;
using Orion.Protocol.Types;

public sealed class PlayerPlaceBlockSignal : PlayerSignal, ICancellable
{
    public override ServerEvent Event => ServerEvent.PlayerPlaceBlock;
    public BlockPos BlockPosition { get; }
    public int BlockFace { get; }
    public bool Cancelled { get; private set; }

    public PlayerPlaceBlockSignal(Player player, BlockPos blockPosition, int blockFace) : base(player)
    {
        BlockPosition = blockPosition;
        BlockFace = blockFace;
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
