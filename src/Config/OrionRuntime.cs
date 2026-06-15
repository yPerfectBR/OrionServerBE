namespace Orion.Config;

/// <summary>
/// Applies process-wide runtime limits from config/server.json.
/// </summary>
public static class OrionRuntime
{
    public static void Apply(RuntimeConfig runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ApplyThreadPool(runtime.ThreadPool);
    }

    /// <summary>
    /// Validates configured thread-pool limits against dedicated scheduling requirements.
    /// Returns warning messages when explicit limits are below the recommended minimum.
    /// </summary>
    public static IReadOnlyList<string> ValidateThreadPool(OrionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        SchedulingThreadBudget budget = SchedulingThreadRequirements.Compute(config);
        ThreadPoolConfig threadPool = config.Runtime.ThreadPool;
        List<string> warnings = [];

        if (threadPool.MaxWorkerThreads > 0 && threadPool.MaxWorkerThreads < budget.MinimumThreadPoolMaxWorkers)
        {
            warnings.Add(
                $"Runtime.ThreadPool.MaxWorkerThreads ({threadPool.MaxWorkerThreads}) is below the recommended minimum of " +
                $"{budget.MinimumThreadPoolMaxWorkers} for {budget.AreaWorkerCount} area worker(s) and " +
                $"{budget.SessionWorkerCount} session worker(s). Increase MaxWorkerThreads or set it to 0 to keep the .NET default.");
        }

        if (threadPool.MinWorkerThreads > 0 && threadPool.MinWorkerThreads > budget.MinimumThreadPoolMaxWorkers)
        {
            warnings.Add(
                $"Runtime.ThreadPool.MinWorkerThreads ({threadPool.MinWorkerThreads}) exceeds the dedicated scheduling budget of " +
                $"{budget.DedicatedWorkerThreads} worker thread(s). This may reserve more pool threads than the server needs.");
        }

        return warnings;
    }

    public static void ApplyThreadPool(ThreadPoolConfig threadPool)
    {
        ArgumentNullException.ThrowIfNull(threadPool);

        ThreadPool.GetMinThreads(out int currentMinWorkers, out int currentMinIo);
        ThreadPool.GetMaxThreads(out int currentMaxWorkers, out int currentMaxIo);

        int minWorkers = threadPool.MinWorkerThreads > 0 ? threadPool.MinWorkerThreads : currentMinWorkers;
        int minIo = threadPool.MinIoCompletionThreads > 0 ? threadPool.MinIoCompletionThreads : currentMinIo;
        int maxWorkers = threadPool.MaxWorkerThreads > 0 ? threadPool.MaxWorkerThreads : currentMaxWorkers;
        int maxIo = threadPool.MaxIoCompletionThreads > 0 ? threadPool.MaxIoCompletionThreads : currentMaxIo;

        if (maxWorkers < minWorkers)
        {
            throw new InvalidOperationException(
                $"Runtime.ThreadPool.MaxWorkerThreads ({maxWorkers}) must be >= MinWorkerThreads ({minWorkers}).");
        }

        if (maxIo < minIo)
        {
            throw new InvalidOperationException(
                $"Runtime.ThreadPool.MaxIoCompletionThreads ({maxIo}) must be >= MinIoCompletionThreads ({minIo}).");
        }

        if (threadPool.MinWorkerThreads > 0 || threadPool.MinIoCompletionThreads > 0)
        {
            if (!ThreadPool.SetMinThreads(minWorkers, minIo))
            {
                throw new InvalidOperationException(
                    $"Failed to set thread pool minimums to worker={minWorkers}, io={minIo}.");
            }
        }

        if (threadPool.MaxWorkerThreads > 0 || threadPool.MaxIoCompletionThreads > 0)
        {
            if (!ThreadPool.SetMaxThreads(maxWorkers, maxIo))
            {
                throw new InvalidOperationException(
                    $"Failed to set thread pool maximums to worker={maxWorkers}, io={maxIo}.");
            }
        }
    }
}
