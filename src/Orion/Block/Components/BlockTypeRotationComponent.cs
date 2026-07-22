namespace Orion.Block.Components;

using Orion.Api.Blocks;


public sealed class BlockTypeRotationComponent : BlockTypeComponent
{
    public new static string Identifier => "minecraft:rotation";

    public static CardinalDirection GetCardinalDirection(float yaw) =>
        BlockRotation.GetCardinalDirection(yaw);

    public static float NormalizeYaw(float yaw) =>
        BlockRotation.NormalizeYaw(yaw);
}
