using Orion.Config;
using Orion.PluginContracts;
using Orion.PluginContracts.Diagnostics;
using Orion.PluginContracts.Events;
using Orion.PluginContracts.Network;
using Orion.Plugins.Diagnostics;
using Orion.RakNet;
using Log = Orion.Logger.Logger;

namespace Orion.Plugins.Network;

public sealed class PacketPipeline : IPacketPipeline
{
    readonly object _sync = new();
    readonly Dictionary<int, List<ReceiveHookEntry>> _receiveById = [];
    readonly List<ReceiveHookEntry> _receiveAll = [];
    readonly Dictionary<int, List<SendHookEntry>> _sendById = [];
    readonly List<SendHookEntry> _sendAll = [];
    readonly Dictionary<int, OwnerEntry> _owners = [];
    readonly HashSet<string> _warnedSubscribeAll = new(StringComparer.Ordinal);
    PluginDiagnostics? _diagnostics;
    int _nextSequence;
    int _receiveAllCount;
    int _sendAllCount;
    int _receiveHookCount;
    int _sendHookCount;
    int _ownerCount;

    public void SetDiagnostics(PluginDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        _diagnostics = diagnostics;
    }

    public bool HasReceiveInterest(int packetId)
    {
        if (_receiveHookCount == 0 && _ownerCount == 0)
        {
            return false;
        }

        lock (_sync)
        {
            return _receiveAllCount > 0
                || _owners.ContainsKey(packetId)
                || (_receiveById.TryGetValue(packetId, out List<ReceiveHookEntry>? list) && list.Count > 0);
        }
    }

    public bool HasSendInterest(int packetId)
    {
        if (_sendHookCount == 0)
        {
            return false;
        }

        lock (_sync)
        {
            return _sendAllCount > 0
                || (_sendById.TryGetValue(packetId, out List<SendHookEntry>? list) && list.Count > 0);
        }
    }

    public void OnReceive(PacketReceiveHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        ArgumentNullException.ThrowIfNull(hook.Plugin);
        ArgumentNullException.ThrowIfNull(hook.Handler);

        lock (_sync)
        {
            ReceiveHookEntry entry = new(
                hook.Plugin.Id,
                hook.PacketIdFilter,
                hook.Priority,
                hook.Handler,
                Interlocked.Increment(ref _nextSequence));

            if (hook.PacketIdFilter is null)
            {
                WarnSubscribeAllOnce(hook.Plugin.Id);
                _receiveAll.Add(entry);
                _receiveAllCount = _receiveAll.Count;
            }
            else
            {
                int id = hook.PacketIdFilter.Value;
                if (!_receiveById.TryGetValue(id, out List<ReceiveHookEntry>? list))
                {
                    list = [];
                    _receiveById[id] = list;
                }

                list.Add(entry);
            }

            _receiveHookCount++;
        }
    }

    public void OnSend(PacketSendHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        ArgumentNullException.ThrowIfNull(hook.Plugin);
        ArgumentNullException.ThrowIfNull(hook.Handler);

        lock (_sync)
        {
            SendHookEntry entry = new(
                hook.Plugin.Id,
                hook.PacketIdFilter,
                hook.Priority,
                hook.Handler,
                Interlocked.Increment(ref _nextSequence));

            if (hook.PacketIdFilter is null)
            {
                WarnSubscribeAllOnce(hook.Plugin.Id);
                _sendAll.Add(entry);
                _sendAllCount = _sendAll.Count;
            }
            else
            {
                int id = hook.PacketIdFilter.Value;
                if (!_sendById.TryGetValue(id, out List<SendHookEntry>? list))
                {
                    list = [];
                    _sendById[id] = list;
                }

                list.Add(entry);
            }

            _sendHookCount++;
        }
    }

    public bool TryOwnHandler(int packetId, IOrionPlugin owner, PacketHandlerDelegate handler)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(handler);

        string? existingOwnerId;
        lock (_sync)
        {
            if (_owners.TryGetValue(packetId, out OwnerEntry existing))
            {
                if (string.Equals(existing.OwnerId, owner.Id, StringComparison.Ordinal))
                {
                    _owners[packetId] = existing with { Handler = handler };
                    return true;
                }

                existingOwnerId = existing.OwnerId;
            }
            else
            {
                _owners[packetId] = new OwnerEntry(owner.Id, handler);
                _ownerCount = _owners.Count;
                return true;
            }
        }

        string key = packetId.ToString();
        string message =
            $"TryOwnHandler rejected for packet {packetId}: owned by '{existingOwnerId}', claimed by '{owner.Id}'.";
        if (_diagnostics is not null)
        {
            _diagnostics.Report(new PluginConflict("packet.owner", key, existingOwnerId, owner.Id, message));
        }
        else
        {
            Log.Warn(LogCategory.System, message);
        }

        return false;
    }

    public void RemovePlugin(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        lock (_sync)
        {
            _receiveAll.RemoveAll(e => string.Equals(e.PluginId, pluginId, StringComparison.Ordinal));
            _receiveAllCount = _receiveAll.Count;
            foreach (int key in _receiveById.Keys.ToArray())
            {
                List<ReceiveHookEntry> list = _receiveById[key];
                list.RemoveAll(e => string.Equals(e.PluginId, pluginId, StringComparison.Ordinal));
                if (list.Count == 0)
                {
                    _receiveById.Remove(key);
                }
            }

            _sendAll.RemoveAll(e => string.Equals(e.PluginId, pluginId, StringComparison.Ordinal));
            _sendAllCount = _sendAll.Count;
            foreach (int key in _sendById.Keys.ToArray())
            {
                List<SendHookEntry> list = _sendById[key];
                list.RemoveAll(e => string.Equals(e.PluginId, pluginId, StringComparison.Ordinal));
                if (list.Count == 0)
                {
                    _sendById.Remove(key);
                }
            }

            foreach (int key in _owners.Keys.ToArray())
            {
                if (string.Equals(_owners[key].OwnerId, pluginId, StringComparison.Ordinal))
                {
                    _owners.Remove(key);
                }
            }

            _ownerCount = _owners.Count;
            _receiveHookCount = _receiveAll.Count + _receiveById.Values.Sum(l => l.Count);
            _sendHookCount = _sendAll.Count + _sendById.Values.Sum(l => l.Count);
            _warnedSubscribeAll.Remove(pluginId);
        }
    }

    public void ResetForTests()
    {
        lock (_sync)
        {
            _receiveById.Clear();
            _receiveAll.Clear();
            _sendById.Clear();
            _sendAll.Clear();
            _owners.Clear();
            _warnedSubscribeAll.Clear();
            _receiveAllCount = 0;
            _sendAllCount = 0;
            _receiveHookCount = 0;
            _sendHookCount = 0;
            _ownerCount = 0;
            _nextSequence = 0;
        }
    }

    /// <summary>Dispatch receive hooks + optional owner. Returns false if core should skip (cancelled or handled).</summary>
    public bool DispatchReceive(PacketReceiveContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ReceiveHookEntry[] hooks = SnapshotReceive(context.PacketId);
        InvokeReceive(hooks, context);
        if (context.Cancelled)
        {
            return false;
        }

        OwnerEntry? owner;
        lock (_sync)
        {
            owner = _owners.TryGetValue(context.PacketId, out OwnerEntry entry) ? entry : null;
        }

        if (owner is not null)
        {
            try
            {
                owner.Value.Handler(context);
            }
            catch (Exception exception)
            {
                Log.Warn(
                    LogCategory.System,
                    "Packet owner handler for {0} failed: {1}",
                    context.PacketId,
                    exception.Message);
            }

            if (context.Cancelled || context.Handled)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Dispatch send hooks. Returns false if cancelled (skip packet).
    /// On success, <paramref name="payload"/> may be replaced.
    /// </summary>
    public bool DispatchSend(PacketSendContext context, out ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(context);
        SendHookEntry[] hooks = SnapshotSend(context.PacketId);
        InvokeSend(hooks, context);
        if (context.Cancelled)
        {
            payload = default;
            return false;
        }

        payload = context.ReplacementPayload is not null
            ? context.ReplacementPayload
            : context.Payload;
        return true;
    }

    ReceiveHookEntry[] SnapshotReceive(int packetId)
    {
        lock (_sync)
        {
            int count = _receiveAll.Count;
            if (_receiveById.TryGetValue(packetId, out List<ReceiveHookEntry>? list))
            {
                count += list.Count;
            }

            if (count == 0)
            {
                return [];
            }

            ReceiveHookEntry[] snapshot = new ReceiveHookEntry[count];
            int i = 0;
            foreach (ReceiveHookEntry entry in _receiveAll)
            {
                snapshot[i++] = entry;
            }

            if (list is not null)
            {
                foreach (ReceiveHookEntry entry in list)
                {
                    snapshot[i++] = entry;
                }
            }

            Array.Sort(snapshot, static (a, b) =>
            {
                int cmp = ComparePriority(a.Priority, b.Priority);
                return cmp != 0 ? cmp : a.Sequence.CompareTo(b.Sequence);
            });
            return snapshot;
        }
    }

    SendHookEntry[] SnapshotSend(int packetId)
    {
        lock (_sync)
        {
            int count = _sendAll.Count;
            if (_sendById.TryGetValue(packetId, out List<SendHookEntry>? list))
            {
                count += list.Count;
            }

            if (count == 0)
            {
                return [];
            }

            SendHookEntry[] snapshot = new SendHookEntry[count];
            int i = 0;
            foreach (SendHookEntry entry in _sendAll)
            {
                snapshot[i++] = entry;
            }

            if (list is not null)
            {
                foreach (SendHookEntry entry in list)
                {
                    snapshot[i++] = entry;
                }
            }

            Array.Sort(snapshot, static (a, b) =>
            {
                int cmp = ComparePriority(a.Priority, b.Priority);
                return cmp != 0 ? cmp : a.Sequence.CompareTo(b.Sequence);
            });
            return snapshot;
        }
    }

    static void InvokeReceive(ReceiveHookEntry[] hooks, PacketReceiveContext context)
    {
        for (int i = 0; i < hooks.Length; i++)
        {
            ReceiveHookEntry entry = hooks[i];
            if (entry.Priority == EventPriority.Monitor)
            {
                bool before = context.Cancelled;
                try
                {
                    entry.Handler(context);
                }
                catch (Exception exception)
                {
                    Log.Warn(LogCategory.System, "Packet receive Monitor hook failed: {0}", exception.Message);
                }

                if (!before && context.Cancelled)
                {
                    context.SetCancelled(false);
                    Log.Warn(
                        LogCategory.System,
                        "Monitor receive hook for packet {0} called Cancel(); cancel was ignored.",
                        context.PacketId);
                }

                continue;
            }

            try
            {
                entry.Handler(context);
            }
            catch (Exception exception)
            {
                Log.Warn(LogCategory.System, "Packet receive hook failed: {0}", exception.Message);
            }
        }
    }

    static void InvokeSend(SendHookEntry[] hooks, PacketSendContext context)
    {
        for (int i = 0; i < hooks.Length; i++)
        {
            SendHookEntry entry = hooks[i];
            if (entry.Priority == EventPriority.Monitor)
            {
                bool before = context.Cancelled;
                try
                {
                    entry.Handler(context);
                }
                catch (Exception exception)
                {
                    Log.Warn(LogCategory.System, "Packet send Monitor hook failed: {0}", exception.Message);
                }

                if (!before && context.Cancelled)
                {
                    context.SetCancelled(false);
                    Log.Warn(
                        LogCategory.System,
                        "Monitor send hook for packet {0} called Cancel(); cancel was ignored.",
                        context.PacketId);
                }

                continue;
            }

            try
            {
                entry.Handler(context);
            }
            catch (Exception exception)
            {
                Log.Warn(LogCategory.System, "Packet send hook failed: {0}", exception.Message);
            }
        }
    }

    static int ComparePriority(EventPriority a, EventPriority b)
    {
        int rankA = a == EventPriority.Monitor ? int.MinValue : (int)a;
        int rankB = b == EventPriority.Monitor ? int.MinValue : (int)b;
        return rankB.CompareTo(rankA);
    }

    void WarnSubscribeAllOnce(string pluginId)
    {
        if (_warnedSubscribeAll.Add(pluginId))
        {
            Log.Warn(
                LogCategory.System,
                "Plugin '{0}' registered a packet hook without PacketIdFilter (subscribe-all). Prefer filtering by PacketId.",
                pluginId);
        }
    }

    readonly record struct ReceiveHookEntry(
        string PluginId,
        int? PacketIdFilter,
        EventPriority Priority,
        Action<PacketReceiveContext> Handler,
        int Sequence);

    readonly record struct SendHookEntry(
        string PluginId,
        int? PacketIdFilter,
        EventPriority Priority,
        Action<PacketSendContext> Handler,
        int Sequence);

    readonly record struct OwnerEntry(string OwnerId, PacketHandlerDelegate Handler);
}

internal sealed class PlayerConnectionAdapter(NetworkConnection connection) : IPlayerConnection
{
    public object? Native { get; } = connection;
}
