using Orion.PluginContracts;
using Orion.PluginContracts.Diagnostics;
using Orion.PluginContracts.Services;
using Orion.Plugins.Diagnostics;

namespace Orion.Plugins.Services;

public sealed class ServiceRegistry : IServiceRegistry
{
    readonly object _sync = new();
    readonly Dictionary<Type, List<ServiceEntry>> _entries = [];
    PluginDiagnostics? _diagnostics;
    int _nextSequence;

    public void SetDiagnostics(PluginDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        _diagnostics = diagnostics;
    }

    public void Register<TService>(TService instance, IOrionPlugin owner, ServicePriority priority = ServicePriority.Normal)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(owner);

        PluginConflict? conflict = null;
        lock (_sync)
        {
            Type type = typeof(TService);
            if (!_entries.TryGetValue(type, out List<ServiceEntry>? list))
            {
                list = [];
                _entries[type] = list;
            }

            if (list.Count > 0)
            {
                ServiceEntry winner = PickBest(list);
                if (!string.Equals(winner.OwnerId, owner.Id, StringComparison.Ordinal))
                {
                    conflict = new PluginConflict(
                        "service",
                        type.Name,
                        winner.OwnerId,
                        owner.Id,
                        $"Multiple providers for '{type.Name}': '{winner.OwnerId}' wins by priority; '{owner.Id}' also registered.");
                }
            }

            list.Add(new ServiceEntry(instance, owner.Id, priority, Interlocked.Increment(ref _nextSequence)));
        }

        if (conflict is not null)
        {
            _diagnostics?.Report(conflict, applyConflictMode: false);
        }
    }

    public void UnregisterAll(IOrionPlugin owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        UnregisterAllByOwnerId(owner.Id);
    }

    public void UnregisterAllByOwnerId(string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        lock (_sync)
        {
            foreach (Type type in _entries.Keys.ToArray())
            {
                List<ServiceEntry> list = _entries[type];
                list.RemoveAll(e => string.Equals(e.OwnerId, ownerId, StringComparison.Ordinal));
                if (list.Count == 0)
                {
                    _entries.Remove(type);
                }
            }
        }
    }

    public bool TryGet<TService>(out TService? service) where TService : class
    {
        lock (_sync)
        {
            if (!_entries.TryGetValue(typeof(TService), out List<ServiceEntry>? list) || list.Count == 0)
            {
                service = null;
                return false;
            }

            service = (TService)PickBest(list).Instance;
            return true;
        }
    }

    static ServiceEntry PickBest(List<ServiceEntry> list)
    {
        ServiceEntry best = list[0];
        for (int i = 1; i < list.Count; i++)
        {
            ServiceEntry candidate = list[i];
            int cmp = ((int)candidate.Priority).CompareTo((int)best.Priority);
            if (cmp > 0 || (cmp == 0 && candidate.Sequence < best.Sequence))
            {
                best = candidate;
            }
        }

        return best;
    }

    public TService GetRequired<TService>() where TService : class
    {
        if (TryGet(out TService? service) && service is not null)
        {
            return service;
        }

        throw new InvalidOperationException(
            $"No service registered for type '{typeof(TService).FullName}'.");
    }

    public IReadOnlyList<string> ListServiceTypeNames()
    {
        lock (_sync)
        {
            return _entries.Keys
                .Select(t => t.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public void ResetForTests()
    {
        lock (_sync)
        {
            _entries.Clear();
            _nextSequence = 0;
        }
    }

    readonly record struct ServiceEntry(object Instance, string OwnerId, ServicePriority Priority, int Sequence);
}
