using Orion.Config;
using WorldLogger = Orion.Logger.Logger;

namespace Orion.World.Pregeneration;

public sealed class ChunkPregenerator
{
    private const int ProgressLogInterval = 10_000;

    public void Pregenerate(Dimension dimension, ChunkPregenerationConfig region, string? dimensionIdentifier = null)
    {
        ArgumentNullException.ThrowIfNull(dimension);
        ArgumentNullException.ThrowIfNull(region);

        int startX = region.Start[0];
        int startZ = region.Start[1];
        int endX = region.End[0];
        int endZ = region.End[1];
        long width = endX - startX + 1L;
        long depth = endZ - startZ + 1L;
        long total = width * depth;
        long processed = 0;
        string label = dimensionIdentifier ?? dimension.Identifier;

        WorldLogger.Info(
            LogCategory.World,
            "Pregenerating dimension '{0}' chunks [{1},{2}]..[{3},{4}] ({5} chunks, memoryLock={6})",
            label,
            startX,
            startZ,
            endX,
            endZ,
            total,
            region.MemoryLock);

        for (int x = startX; x <= endX; x++)
        {
            for (int z = startZ; z <= endZ; z++)
            {
                dimension.GetOrCreateChunk(x, z);
                dimension.SaveChunk(x, z);

                if (!region.MemoryLock)
                {
                    dimension.UnloadChunk(x, z, save: false);
                }

                processed++;
                if (processed % ProgressLogInterval == 0 || processed == total)
                {
                    WorldLogger.Info(
                        LogCategory.World,
                        "Pregenerating dimension '{0}': {1}/{2} chunks ({3:P1})",
                        label,
                        processed,
                        total,
                        total == 0 ? 1d : processed / (double)total);
                }
            }
        }
    }

    public void PregenerateAll(Dimension dimension, IReadOnlyList<ChunkPregenerationConfig> regions, string? dimensionIdentifier = null)
    {
        for (int i = 0; i < regions.Count; i++)
        {
            Pregenerate(dimension, regions[i], dimensionIdentifier);
        }
    }
}
