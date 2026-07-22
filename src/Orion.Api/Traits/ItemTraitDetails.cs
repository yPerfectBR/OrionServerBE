using Orion.Api.Math;

namespace Orion.Api.Traits;

public readonly record struct ItemUseOnAirDetails(IPlayer Player, int HotBarSlot, Vec3f Position);

public readonly record struct ItemUseOnBlockDetails(
    IPlayer Player,
    int HotBarSlot,
    BlockPos BlockPosition,
    int BlockFace,
    Vec3f Position,
    Vec3f ClickedPosition);

public readonly record struct ItemPlaceDetails(
    IPlayer Player,
    int HotBarSlot,
    BlockPos BlockPosition,
    int BlockFace,
    Vec3f Position,
    Vec3f ClickedPosition);

public readonly record struct ItemUseOnEntityDetails(
    IPlayer Player,
    IEntity Target,
    int HotBarSlot,
    Vec3f Position,
    Vec3f ClickedPosition);

public readonly record struct ItemUseAttackDetails(
    IPlayer Player,
    IEntity Target,
    int HotBarSlot,
    Vec3f Position,
    Vec3f ClickedPosition);

public readonly record struct ItemBreakBlockDetails(
    IPlayer Player,
    int HotBarSlot,
    BlockPos BlockPosition,
    int BlockFace);
