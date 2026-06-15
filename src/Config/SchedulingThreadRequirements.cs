namespace Orion.Config;

/// <summary>
/// Thread counts derived from world threading areas and session scheduling.
/// </summary>
public readonly record struct SchedulingThreadBudget(
    bool AreaThreadingEnabled,
    int AreaWorkerCount,
    int SessionWorkerCount,
    int DedicatedWorkerThreads,
    int MinimumThreadPoolMaxWorkers);

/// <summary>
/// Computes how many dedicated scheduling threads the server needs from <see cref="OrionConfig"/>.
/// </summary>
public static class SchedulingThreadRequirements
{
    /// <summary>One session worker is enough to process all player sessions sequentially.</summary>
    public const int MinimumSessionWorkerCount = 1;

    public static SchedulingThreadBudget Compute(OrionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        bool areaThreading = config.Server.WorldDefaultSettings.Dimensions
            .Any(d => d.ThreadingAreas is { Count: > 0 });

        int areaWorkers = areaThreading
            ? CountAreaWorkersFromConfig(config)
            : 1;
        int sessionWorkers = areaThreading ? MinimumSessionWorkerCount : 0;
        int dedicatedWorkers = areaWorkers + sessionWorkers;

        return new SchedulingThreadBudget(
            areaThreading,
            areaWorkers,
            sessionWorkers,
            dedicatedWorkers,
            dedicatedWorkers);
    }

    public static int CountAreaWorkersFromConfig(OrionConfig config)
    {
        int max = 1;
        foreach (DimensionConfig dimension in config.Server.WorldDefaultSettings.Dimensions)
        {
            max = Math.Max(max, (dimension.ThreadingAreas?.Count ?? 0) + 1);
        }

        return max;
    }
}
