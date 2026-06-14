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
