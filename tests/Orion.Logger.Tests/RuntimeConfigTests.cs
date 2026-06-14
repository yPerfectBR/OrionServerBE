using Orion.Config;

namespace Orion.Logger.Tests;

public sealed class RuntimeConfigTests
{
    [Fact]
    public void LoadServerJson_AppliesThreadPoolLimits()
    {
        string configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "config", "server.json"));

        OrionInfo.Load(configPath);

        Assert.Equal(4, OrionInfo.Runtime.ThreadPool.MaxWorkerThreads);
        Assert.Equal(4, OrionInfo.Runtime.ThreadPool.MaxIoCompletionThreads);

        ThreadPool.GetMaxThreads(out int maxWorkers, out int maxIo);
        Assert.Equal(4, maxWorkers);
        Assert.Equal(4, maxIo);
    }

    [Fact]
    public void ApplyThreadPool_RejectsMaxBelowMin()
    {
        var runtime = new RuntimeConfig
        {
            ThreadPool = new ThreadPoolConfig
            {
                MinWorkerThreads = 8,
                MaxWorkerThreads = 4
            }
        };

        Assert.Throws<InvalidOperationException>(() => OrionRuntime.Apply(runtime));
    }
}
