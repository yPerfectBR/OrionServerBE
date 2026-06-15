namespace Orion.Block.Traits.Types;


using Orion.Protocol.Types;


public readonly record struct BlockPlaceDetails(Orion.Player.Player Player, BlockPos BlockPosition, int BlockFace, Vec3f ClickedPosition);







