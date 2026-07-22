using Orion.Api.Math;

namespace Orion.Api.Traits;

/// <summary>Details passed to <see cref="BlockTraitBase.OnPlace"/>.</summary>
public readonly record struct BlockPlaceDetails(
    IPlayer Player,
    BlockPos BlockPosition,
    int BlockFace,
    Vec3f ClickedPosition);
