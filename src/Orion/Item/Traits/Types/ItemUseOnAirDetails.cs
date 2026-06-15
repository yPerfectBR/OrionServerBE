namespace Orion.Item.Traits.Types;

using Player = Orion.Player.Player;
using Orion.Protocol.Types;


public readonly record struct ItemUseOnAirDetails(Player Player, int HotBarSlot, Vec3f Position);







