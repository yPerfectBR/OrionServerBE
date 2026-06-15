namespace Orion.World.Coordinates;

/// <summary>
/// Shared chunk/region coordinate math.
/// </summary>
public static class CoordMath
{
    /// <summary>
    /// Floor division: matches Minecraft chunk indexing for negative coordinates.
    /// </summary>
    public static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
        {
            quotient--;
        }

        return quotient;
    }

    public static long HashChunk(int x, int z) => ((long)x << 32) | (uint)z;

    public static void UnhashChunk(long hash, out int x, out int z)
    {
        x = (int)(hash >> 32);
        z = (int)hash;
    }
}
