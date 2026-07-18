namespace Orion.World.Coordinates;

/// <summary>
/// Bedrock client view-distance math (matches Geyser ChunkUtils.squareToCircle).
/// Server streaming stays Chebyshev; the client culls with a circle, so the
/// negotiated/publisher radius must be padded.
/// </summary>
public static class ChunkViewMath
{
    public const int MaxBedrockViewDistance = 96;

    /// <summary>
    /// Converts a square (Chebyshev) chunk render distance to the circular
    /// chunk radius Bedrock expects in ChunkRadiusUpdated / publisher.
    /// </summary>
    public static int SquareToCircle(int renderDistance)
    {
        int padded = (int)Math.Ceiling((renderDistance + 1) * Math.Sqrt(2.0));
        return Math.Clamp(padded, 1, MaxBedrockViewDistance);
    }

    /// <summary>
    /// Largest Chebyshev stream radius whose circular padding still fits in
    /// <paramref name="clientMaxCircle"/>. Sending SquareToCircle(R) above the
    /// client's MaxChunkRadius can crash devices; clamping the circle instead
    /// reintroduces corner voids.
    /// </summary>
    public static int MaxChebyshevForClientCircle(int clientMaxCircle)
    {
        int limit = Math.Clamp(clientMaxCircle, 1, MaxBedrockViewDistance);
        // SquareToCircle(R) = ceil((R+1)*sqrt(2)) <= limit
        // ⇒ R+1 <= limit/sqrt(2) ⇒ R <= floor(limit/sqrt(2) - 1) (then verify)
        int radius = (int)Math.Floor(limit / Math.Sqrt(2.0) - 1.0);
        radius = Math.Clamp(radius, 1, limit);
        while (radius < limit && SquareToCircle(radius + 1) <= limit)
        {
            radius++;
        }

        while (radius > 1 && SquareToCircle(radius) > limit)
        {
            radius--;
        }

        return radius;
    }

    /// <summary>
    /// Publisher radius in blocks: squareToCircle(renderDistance) &lt;&lt; 4.
    /// </summary>
    public static uint PublisherRadiusBlocks(int renderDistance) =>
        (uint)(SquareToCircle(renderDistance) << 4);
}
