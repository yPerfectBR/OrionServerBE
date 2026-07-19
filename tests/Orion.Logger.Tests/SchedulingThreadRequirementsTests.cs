using Orion.Config;

namespace Orion.Logger.Tests;

public sealed class SchedulingThreadRequirementsTests
{
    [Fact]
    public void Compute_DefaultServerJson_RequiresThreeDedicatedWorkers()
    {
        string configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "config", "server.json"));
        OrionInfo.Load(configPath);

        SchedulingThreadBudget budget = SchedulingThreadRequirements.Compute(OrionInfo.Config);

        Assert.True(budget.AreaThreadingEnabled);
        Assert.Equal(2, budget.AreaWorkerCount);
        Assert.Equal(1, budget.SessionWorkerCount);
        Assert.Equal(3, budget.DedicatedWorkerThreads);
        Assert.Equal(3, budget.MinimumThreadPoolMaxWorkers);
    }

    [Fact]
    public void Compute_ScalesAreaWorkersWithThreadingAreaCount()
    {
        var config = new OrionConfig
        {
            Server = new ServerSection
            {
                WorldDefaultSettings = new WorldProperties
                {
                    Dimensions =
                    [
                        new DimensionConfig
                        {
                            ThreadingAreas =
                            [
                                new ThreadingAreaConfig(),
                                new ThreadingAreaConfig(),
                                new ThreadingAreaConfig()
                            ]
                        }
                    ]
                }
            }
        };

        SchedulingThreadBudget budget = SchedulingThreadRequirements.Compute(config);

        Assert.Equal(4, budget.AreaWorkerCount);
        Assert.Equal(1, budget.SessionWorkerCount);
        Assert.Equal(5, budget.DedicatedWorkerThreads);
    }

    [Fact]
    public void ValidateThreadPool_WarnsWhenMaxWorkersBelowDedicatedBudget()
    {
        var config = new OrionConfig
        {
            Runtime = new RuntimeConfig
            {
                ThreadPool = new ThreadPoolConfig
                {
                    MaxWorkerThreads = 2
                }
            },
            Server = new ServerSection
            {
                WorldDefaultSettings = new WorldProperties
                {
                    Dimensions =
                    [
                        new DimensionConfig
                        {
                            ThreadingAreas = [new ThreadingAreaConfig()]
                        }
                    ]
                }
            }
        };

        IReadOnlyList<string> warnings = OrionRuntime.ValidateThreadPool(config);

        Assert.Contains(warnings, warning => warning.Contains("MaxWorkerThreads (2)"));
        Assert.Contains(warnings, warning => warning.Contains("recommended minimum of 3"));
    }

    [Fact]
    public void ValidateThreadPool_AllowsUnsetMaxWorkers()
    {
        var config = new OrionConfig
        {
            Runtime = new RuntimeConfig
            {
                ThreadPool = new ThreadPoolConfig
                {
                    MaxWorkerThreads = 0
                }
            },
            Server = new ServerSection
            {
                WorldDefaultSettings = new WorldProperties
                {
                    Dimensions =
                    [
                        new DimensionConfig
                        {
                            ThreadingAreas = [new ThreadingAreaConfig()]
                        }
                    ]
                }
            }
        };

        IReadOnlyList<string> warnings = OrionRuntime.ValidateThreadPool(config);

        Assert.DoesNotContain(warnings, warning => warning.Contains("MaxWorkerThreads"));
    }

    [Fact]
    public void ValidateThreadPool_NoWarningWhenMaxWorkersMeetsBudget()
    {
        string configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "config", "server.json"));
        OrionInfo.Load(configPath);

        IReadOnlyList<string> warnings = OrionRuntime.ValidateThreadPool(OrionInfo.Config);

        Assert.DoesNotContain(warnings, warning => warning.Contains("MaxWorkerThreads"));
    }
}
