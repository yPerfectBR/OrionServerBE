namespace Orion.Item.Traits.Types;

using Player = Orion.Player.Player;
using Orion.Protocol.Types;


public readonly record struct ItemBreakBlockDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace);







