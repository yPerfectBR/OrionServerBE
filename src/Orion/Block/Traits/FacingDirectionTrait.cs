namespace Orion.Block.Traits;

using Orion.Api.Blocks;
using Orion.Api.Traits;
using Orion.Block.Components;


public sealed class FacingDirectionTrait : DirectionTrait
{
    public static new readonly string Identifier = "facing_direction";
    public static new readonly string State = "facing_direction";
    public static new readonly Type? Component = typeof(BlockTypeRotationComponent);

    public FacingDirectionTrait(Block block) : base(block)
    {
    }

    public override void OnPlace(BlockPlaceDetails details)
    {
        int pitch = (int)MathF.Ceiling(details.Player.Pitch);
        if (pitch == 90)
        {
            SetDirection(FacingDirection.Up);
            return;
        }

        if (pitch == -90)
        {
            SetDirection(FacingDirection.Down);
            return;
        }

        CardinalDirection direction = BlockRotation.GetCardinalDirection(details.Player.Yaw);
        switch (direction)
        {
            case CardinalDirection.North:
                SetDirection(FacingDirection.South);
                break;
            case CardinalDirection.South:
                SetDirection(FacingDirection.North);
                break;
            case CardinalDirection.East:
                SetDirection(FacingDirection.West);
                break;
            case CardinalDirection.West:
                SetDirection(FacingDirection.East);
                break;
        }
    }

    public new FacingDirection GetDirection()
    {
        return Block.TryGetStateInt(State, out int value)
            ? (FacingDirection)value
            : FacingDirection.South;
    }

    public void SetDirection(FacingDirection direction) =>
        Block.SetStateInt(State, (int)direction);
}
