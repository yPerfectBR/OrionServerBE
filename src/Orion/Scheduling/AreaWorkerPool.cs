namespace Orion.Scheduling;

public sealed class AreaWorkerPool : IDisposable
{
    private readonly AreaWorker[] _workers;

    public AreaWorkerPool(int workerCount, Server server)
    {
        if (workerCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Area worker count must be at least 1.");
        }

        _workers = new AreaWorker[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = new AreaWorker(i, server);
        }
    }

    public int WorkerCount => _workers.Length;

    public AreaWorker GetWorker(int id)
    {
        if (id < 0 || id >= _workers.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return _workers[id];
    }

    public IReadOnlyList<AreaWorkerLoadMetrics> GetAllMetrics()
    {
        AreaWorkerLoadMetrics[] metrics = new AreaWorkerLoadMetrics[_workers.Length];
        for (int i = 0; i < _workers.Length; i++)
        {
            metrics[i] = _workers[i].Metrics;
        }

        return metrics;
    }

    public void Start()
    {
        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i].Start();
        }
    }

    public void Stop()
    {
        for (int i = 0; i < _workers.Length; i++)
        {
            _workers[i].Stop();
        }
    }

    public void Dispose() => Stop();
}
