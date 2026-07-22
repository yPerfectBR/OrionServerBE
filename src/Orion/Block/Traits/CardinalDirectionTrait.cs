namespace Orion.Block.Traits;

using Orion.Api.Blocks;
using Orion.Api.Traits;
using Orion.Block.Components;


public sealed class CardinalDirectionTrait : DirectionTrait
{
    public static new readonly string Identifier = "minecraft:cardinal_direction";
    public static new readonly string State = "minecraft:cardinal_direction";
    public static new readonly Type? Component = typeof(BlockTypeRotationComponent);

    public CardinalDirectionTrait(Block block) : base(block)
    {
    }

    public override void OnPlace(BlockPlaceDetails details)
    {
        CardinalDirection direction = BlockRotation.GetCardinalDirection(details.Player.Yaw);

        switch (direction)
        {
            case CardinalDirection.North:
                SetDirection(CardinalDirection.South);
                break;
            case CardinalDirection.South:
                SetDirection(CardinalDirection.North);
                break;
            case CardinalDirection.East:
                SetDirection(CardinalDirection.West);
                break;
            case CardinalDirection.West:
                SetDirection(CardinalDirection.East);
                break;
        }
    }

    public new CardinalDirection GetDirection()
    {
        if (!Block.TryGetStateString(State, out string value))
        {
            return CardinalDirection.South;
        }

        return value switch
        {
            "north" => CardinalDirection.North,
            "south" => CardinalDirection.South,
            "east" => CardinalDirection.East,
            "west" => CardinalDirection.West,
            _ => CardinalDirection.South
        };
    }

    public void SetDirection(CardinalDirection direction)
    {
        string value = direction switch
        {
            CardinalDirection.North => "north",
            CardinalDirection.South => "south",
            CardinalDirection.East => "east",
            CardinalDirection.West => "west",
            _ => "south"
        };

        Block.SetStateString(State, value);
    }
}
