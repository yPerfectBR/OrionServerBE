using Orion.Config;

namespace Orion.World.Threading;

/// <summary>
/// Maps chunk coordinates to worker thread indices.
/// Thread 0 is the default for coordinates outside configured areas.
/// </summary>
public sealed class AreaResolver
{
    public const int DefaultThread = 0;

    private readonly IReadOnlyList<ThreadingAreaConfig> _areas;

    public AreaResolver(IReadOnlyList<ThreadingAreaConfig> areas) =>
        _areas = areas ?? throw new ArgumentNullException(nameof(areas));

    public int ResolveArea(int chunkX, int chunkZ)
    {
        for (int i = 0; i < _areas.Count; i++)
        {
            ThreadingAreaConfig area = _areas[i];
            if (chunkX >= area.Start[0] && chunkX <= area.End[0]
                && chunkZ >= area.Start[1] && chunkZ <= area.End[1])
            {
                return i + 1;
            }
        }

        return DefaultThread;
    }
}
