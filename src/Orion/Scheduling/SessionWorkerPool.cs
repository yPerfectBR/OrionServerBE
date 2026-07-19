namespace Orion.Scheduling;

public sealed class SessionWorkerPool : IDisposable
{
    private readonly SessionWorker[] _workers;

    public SessionWorkerPool(int workerCount, Server server)
    {
        if (workerCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(workerCount), "Session worker count must be at least 1.");
        }

        _workers = new SessionWorker[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = new SessionWorker(i, server);
        }
    }

    public int WorkerCount => _workers.Length;

    public SessionWorker GetWorker(int id)
    {
        if (id < 0 || id >= _workers.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return _workers[id];
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
