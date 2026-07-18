using Orion.Events;
using Orion.PluginContracts.Events;

namespace Orion.Plugins;

/// <summary>Adapts <see cref="Server"/> event dispatch to <see cref="IEventBus"/>.</summary>
public sealed class ServerEventBus(Server server) : IEventBus
{
    readonly Server _server = server ?? throw new ArgumentNullException(nameof(server));

    public void Subscribe<TSignal>(Action<TSignal> handler, EventPriority priority = EventPriority.Normal)
        where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(handler);
        _server.On(SignalEventMap.For<TSignal>(), handler, priority);
    }

    public void Unsubscribe<TSignal>(Action<TSignal> handler) where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(handler);
        _server.Off(SignalEventMap.For<TSignal>(), handler);
    }

    public IDisposable SubscribeDisposable<TSignal>(
        Action<TSignal> handler,
        EventPriority priority = EventPriority.Normal) where TSignal : ISignal
    {
        Subscribe(handler, priority);
        return new Subscription<TSignal>(this, handler);
    }

    sealed class Subscription<TSignal>(ServerEventBus bus, Action<TSignal> handler) : IDisposable
        where TSignal : ISignal
    {
        ServerEventBus? _bus = bus;
        Action<TSignal>? _handler = handler;

        public void Dispose()
        {
            ServerEventBus? bus = Interlocked.Exchange(ref _bus, null);
            Action<TSignal>? handler = Interlocked.Exchange(ref _handler, null);
            if (bus is not null && handler is not null)
            {
                bus.Unsubscribe(handler);
            }
        }
    }
}

/// <summary>Tracks subscriptions so <see cref="PluginHost"/> can unsubscribe on disable.</summary>
internal sealed class TrackingEventBus(IEventBus inner) : IEventBus
{
    readonly IEventBus _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    readonly List<Action> _unsubscribeActions = [];
    readonly object _lock = new();

    public void Subscribe<TSignal>(Action<TSignal> handler, EventPriority priority = EventPriority.Normal)
        where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(handler);
        _inner.Subscribe(handler, priority);
        lock (_lock)
        {
            _unsubscribeActions.Add(() => _inner.Unsubscribe(handler));
        }
    }

    public void Unsubscribe<TSignal>(Action<TSignal> handler) where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(handler);
        _inner.Unsubscribe(handler);
    }

    public IDisposable SubscribeDisposable<TSignal>(
        Action<TSignal> handler,
        EventPriority priority = EventPriority.Normal) where TSignal : ISignal
    {
        Subscribe(handler, priority);
        return new Disposable(() => Unsubscribe(handler));
    }

    public void UnsubscribeAll()
    {
        Action[] actions;
        lock (_lock)
        {
            actions = _unsubscribeActions.ToArray();
            _unsubscribeActions.Clear();
        }

        foreach (Action action in actions)
        {
            try
            {
                action();
            }
            catch
            {
                // Best-effort cleanup on disable.
            }
        }
    }

    sealed class Disposable(Action dispose) : IDisposable
    {
        Action? _dispose = dispose;

        public void Dispose()
        {
            Action? action = Interlocked.Exchange(ref _dispose, null);
            action?.Invoke();
        }
    }
}
