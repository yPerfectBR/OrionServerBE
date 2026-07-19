namespace Orion.Scheduling;

/// <summary>
/// Starts and stops area/session worker pools and the single-thread fallback tick loop.
/// Replaces <see cref="Orion.World.Threading.AreaThreadScheduler"/> for server-side scheduling.
/// </summary>
public sealed class SchedulingBootstrap : IDisposable
{
    private readonly Server _server;
    private readonly Lock _sync = new();
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
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _running = false;
        }

        _server.ConnectionCoordinator?.Stop();
        _server.AreaScheduler.Stop();
        _server.Scheduler.Stop();
    }

    public void Dispose() => Stop();

    public void DrainNetworkQueue() => _server.Scheduler.DrainMainQueue();
}
