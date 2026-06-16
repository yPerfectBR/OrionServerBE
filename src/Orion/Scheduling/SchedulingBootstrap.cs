using System.Diagnostics;
using Orion.World;
using WorldInstance = Orion.World.World;

namespace Orion.Scheduling;

/// <summary>
/// Starts and stops area/session worker pools and the single-thread fallback tick loop.
/// Replaces <see cref="Orion.World.Threading.AreaThreadScheduler"/> for server-side scheduling.
/// </summary>
public sealed class SchedulingBootstrap : IDisposable
{
    private readonly Server _server;
    private readonly Lock _sync = new();
    private Thread? _tickThread;
    private volatile bool _running;

    public SchedulingBootstrap(Server server) =>
        _server = server ?? throw new ArgumentNullException(nameof(server));

    public void Start()
    {
        lock (_sync)
        {
            if (_running)
            {
                return;
            }

            _running = true;
            _server.AreaScheduler.Start();
            _server.ConnectionCoordinator?.Start();
            _server.Scheduler.Start();

            if (!_server.Properties.AreaThreadingEnabled)
            {
                _tickThread = new Thread(TickLoop)
                {
                    IsBackground = true,
                    Name = "orion-scheduler-tick"
                };
                _tickThread.Start();
            }
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _running = false;
        }

        _tickThread?.Join(TimeSpan.FromSeconds(5));
        _tickThread = null;

        _server.ConnectionCoordinator?.Stop();
        _server.AreaScheduler.Stop();
        _server.Scheduler.Stop();
    }

    public void Dispose() => Stop();

    public void DrainNetworkQueue() => _server.Scheduler.DrainMainQueue();

    void TickLoop()
    {
        double intervalMs = 1000.0 / Math.Max(1, _server.Properties.TicksPerSecond);
        Stopwatch sw = Stopwatch.StartNew();
        double nextTickAt = 0;

        while (_running)
        {
            double now = sw.Elapsed.TotalMilliseconds;
            if (now >= nextTickAt)
            {
                Stopwatch work = Stopwatch.StartNew();
                if (_server.World is not null)
                {
                    _server.World.Tick();
                    if (!_server.Properties.SessionThreadingEnabled)
                    {
                        TickEntities(_server.World);
                    }
                }

                _server.Scheduler.DrainMainQueue();
                _server.SetTps(1000.0 / Math.Max(work.Elapsed.TotalMilliseconds, 1));
                nextTickAt += intervalMs;
            }

            Thread.Sleep(1);
        }
    }

    static void TickEntities(WorldInstance world)
    {
        ulong tick = world.TickValue;
        foreach (Orion.World.Dimension dimension in world.Dimensions)
        {
            foreach (global::Orion.Entity.Entity entity in dimension.GetEntities())
            {
                entity.Tick(tick, 1);
            }
        }
    }
}
