using System.Text.RegularExpressions;
using Orion.Config;
using Orion.PluginContracts;
using Orion.PluginContracts.Messaging;
using Log = Orion.Logger.Logger;

namespace Orion.Plugins.Messaging;

public sealed partial class PluginMessenger : IPluginMessenger
{
    readonly object _sync = new();
    readonly Dictionary<string, List<Action<PluginMessage>>> _handlers = new(StringComparer.Ordinal);

    public void Subscribe(string channel, Action<PluginMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateChannel(channel);

        lock (_sync)
        {
            if (!_handlers.TryGetValue(channel, out List<Action<PluginMessage>>? list))
            {
                list = [];
                _handlers[channel] = list;
            }

            list.Add(handler);
        }
    }

    public void Unsubscribe(string channel, Action<PluginMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateChannel(channel);

        lock (_sync)
        {
            if (!_handlers.TryGetValue(channel, out List<Action<PluginMessage>>? list))
            {
                return;
            }

            list.RemoveAll(h => ReferenceEquals(h, handler));
            if (list.Count == 0)
            {
                _handlers.Remove(channel);
            }
        }
    }

    public void Publish(string channel, ReadOnlyMemory<byte> payload, IOrionPlugin? sender = null)
    {
        ValidateChannel(channel);

        Action<PluginMessage>[] snapshot;
        lock (_sync)
        {
            if (!_handlers.TryGetValue(channel, out List<Action<PluginMessage>>? list) || list.Count == 0)
            {
                return;
            }

            snapshot = list.ToArray();
        }

        PluginMessage message = new()
        {
            Channel = channel,
            Payload = payload,
            SenderPluginId = sender?.Id
        };

        foreach (Action<PluginMessage> handler in snapshot)
        {
            try
            {
                handler(message);
            }
            catch (Exception exception)
            {
                Log.Warn(
                    LogCategory.System,
                    "Plugin messenger handler for '{0}' failed: {1}",
                    channel,
                    exception.Message);
            }
        }
    }

    public void ResetForTests()
    {
        lock (_sync)
        {
            _handlers.Clear();
        }
    }

    internal static void ValidateChannel(string channel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        if (!ChannelRegex().IsMatch(channel))
        {
            throw new ArgumentException(
                $"Invalid plugin message channel '{channel}'. Expected namespace:name matching ^[a-z0-9_.-]+:[a-z0-9_./-]+$.",
                nameof(channel));
        }
    }

    [GeneratedRegex(@"^[a-z0-9_.-]+:[a-z0-9_./-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelRegex();
}

/// <summary>Tracks subscriptions so PluginHost can unsubscribe on disable.</summary>
internal sealed class TrackingPluginMessenger(IPluginMessenger inner) : IPluginMessenger
{
    readonly IPluginMessenger _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    readonly List<(string Channel, Action<PluginMessage> Handler)> _subscriptions = [];
    readonly object _lock = new();

    public void Subscribe(string channel, Action<PluginMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _inner.Subscribe(channel, handler);
        lock (_lock)
        {
            _subscriptions.Add((channel, handler));
        }
    }

    public void Unsubscribe(string channel, Action<PluginMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _inner.Unsubscribe(channel, handler);
        lock (_lock)
        {
            _subscriptions.RemoveAll(s =>
                string.Equals(s.Channel, channel, StringComparison.Ordinal)
                && ReferenceEquals(s.Handler, handler));
        }
    }

    public void Publish(string channel, ReadOnlyMemory<byte> payload, IOrionPlugin? sender = null) =>
        _inner.Publish(channel, payload, sender);

    public void UnsubscribeAll()
    {
        (string Channel, Action<PluginMessage> Handler)[] snapshot;
        lock (_lock)
        {
            snapshot = _subscriptions.ToArray();
            _subscriptions.Clear();
        }

        foreach ((string channel, Action<PluginMessage> handler) in snapshot)
        {
            try
            {
                _inner.Unsubscribe(channel, handler);
            }
            catch
            {
                // Best-effort cleanup on disable.
            }
        }
    }
}
