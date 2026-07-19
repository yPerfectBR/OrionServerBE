namespace Orion.Item.Traits.Types;

using Player = Orion.Player.Player;
using Orion.Protocol.Types;


public readonly record struct ItemPlaceDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace, Vec3f Position, Vec3f ClickedPosition);







