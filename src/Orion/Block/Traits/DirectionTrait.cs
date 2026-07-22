namespace Orion.Block.Traits;

using Orion.Api.Blocks;
using Orion.Api.Traits;
using Orion.Block.Components;


public class DirectionTrait : BlockTrait
{
    public static new readonly string Identifier = "direction";
    public static readonly string State = "direction";
    public static new readonly Type? Component = typeof(BlockTypeRotationComponent);

    public DirectionTrait(Block block) : base(block)
    {
    }

    public override void OnPlace(BlockPlaceDetails details)
    {
        CardinalDirection direction = BlockRotation.GetCardinalDirection(details.Player.Yaw);

        switch (direction)
        {
            case CardinalDirection.North:
                SetDirection(2);
                break;
            case CardinalDirection.South:
                SetDirection(0);
                break;
            case CardinalDirection.East:
                SetDirection(3);
                break;
            case CardinalDirection.West:
                SetDirection(1);
                break;
        }
    }

    public int GetDirection()
    {
        return Block.TryGetStateInt(State, out int value) ? value : 0;
    }

    public void SetDirection(int direction) => Block.SetStateInt(State, direction);
}
