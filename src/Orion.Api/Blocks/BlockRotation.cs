namespace Orion.Api.Blocks;

public enum CardinalDirection
{
    East = 0,
    West = 1,
    South = 2,
    North = 3
}

public enum FacingDirection
{
    Down = 0,
    Up = 1,
    North = 2,
    South = 3,
    West = 4,
    East = 5
}

/// <summary>Yaw → cardinal facing helper (matches Bedrock placement conventions).</summary>
public static class BlockRotation
{
    public static CardinalDirection GetCardinalDirection(float yaw)
    {
        float normalized = NormalizeYaw(yaw);

        if (normalized >= 45f && normalized < 135f)
        {
            return CardinalDirection.West;
        }

        if (normalized >= 135f && normalized < 225f)
        {
            return CardinalDirection.North;
        }

        if (normalized >= 225f && normalized < 315f)
        {
            return CardinalDirection.East;
        }

        return CardinalDirection.South;
    }

    public static float NormalizeYaw(float yaw)
    {
        float normalized = yaw % 360f;
        if (normalized < 0f)
        {
            normalized += 360f;
        }

        return normalized;
    }
}
