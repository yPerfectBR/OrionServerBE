using System.Diagnostics;

namespace Orion.World.Threading;

/// <summary>
/// Background world tick scheduler.
/// </summary>
public sealed class AreaThreadScheduler : IDisposable
{
    private readonly World _world;
    private readonly int _ticksPerSecond;
    private readonly Lock _sync = new();
    private Thread? _thread;
    private volatile bool _running;

    public AreaThreadScheduler(World world, int ticksPerSecond = 20)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _ticksPerSecond = Math.Max(1, ticksPerSecond);
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_running)
            {
                return;
            }

            _running = true;
            _thread = new Thread(TickLoop)
            {
                IsBackground = true,
                Name = "orion-world-tick"
            };
            _thread.Start();
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _running = false;
        }

        _thread?.Join(TimeSpan.FromSeconds(5));
        _thread = null;
    }

    public void Dispose() => Stop();

    private void TickLoop()
    {
        double intervalMs = 1000.0 / _ticksPerSecond;
        Stopwatch sw = Stopwatch.StartNew();
        double nextTickAt = 0;

        while (_running)
        {
            double now = sw.Elapsed.TotalMilliseconds;
            if (now >= nextTickAt)
            {
                Stopwatch work = Stopwatch.StartNew();
                _world.Tick();
                _world.TickWork = work.Elapsed.TotalMilliseconds;
                nextTickAt += intervalMs;
            }

            Thread.Sleep(1);
        }
    }
}
