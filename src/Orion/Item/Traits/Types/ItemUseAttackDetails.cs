namespace Orion.Item.Traits.Types;

using Player = Orion.Player.Player;
using Orion.Protocol.Types;


public readonly record struct ItemUseAttackDetails(Player Player, Orion.Entity.Entity Target, int HotBarSlot, Vec3f Position, Vec3f ClickedPosition);







