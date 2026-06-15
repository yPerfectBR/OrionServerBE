using System.Collections.Concurrent;
using Orion.Commands;
using Orion.Events;
using Orion.Network;
using Orion.Player;
using Orion.Protocol.Packets;
using Orion.RakNet;
using Orion.Scheduling;
using WorldInstance = Orion.World.World;

namespace Orion;

public sealed class Server
{
    private readonly INetworkScheduler _scheduler;
    private readonly IAreaScheduler _areaScheduler;
    private readonly ConnectionCoordinator? _connectionCoordinator;
    private readonly Dictionary<ServerEvent, List<Delegate>> _signalHandlers = [];

    public ServerProperties Properties { get; }

    public WorldInstance? World { get; private set; }

    public ConcurrentDictionary<NetworkConnection, PlayerSession> Sessions { get; } = new();

    public NetworkHandler Network { get; }

    public CommandRegistry Commands { get; } = new();

    public INetworkScheduler Scheduler => _scheduler;

    public IAreaScheduler AreaScheduler => _areaScheduler;

    public ConnectionCoordinator? ConnectionCoordinator => _connectionCoordinator;

    public PacketIngress PacketIngress { get; }

    public double Tps { get; private set; } = 20.0;

    public Server(ServerProperties? properties = null)
    {
        Properties = properties ?? new ServerProperties();

        if (Properties.AreaThreadCount < 1)
        {
            throw new InvalidOperationException("area-thread-count must be >= 1.");
        }

        Network = new NetworkHandler(this);
        _scheduler = new SingleThreadScheduler(this);
        _areaScheduler = Properties.AreaThreadingEnabled
            ? new AreaScheduler(this)
            : new NoopAreaScheduler();

        if (Properties.SessionThreadingEnabled)
        {
            if (!Properties.AreaThreadingEnabled)
            {
                throw new InvalidOperationException("session-threading-enabled requires area-threading-enabled.");
            }

            if (Properties.SessionThreadCount < 1)
            {
                throw new InvalidOperationException("session-thread-count must be >= 1.");
            }

            _connectionCoordinator = new ConnectionCoordinator(this, new SessionWorkerPool(Properties.SessionThreadCount, this));
        }

        PacketIngress = new PacketIngress(this);
        Commands.RegisterDefaultCommands();
    }

    public bool TryGetWorld(string identifier, out WorldInstance? world)
    {
        world = World;
        if (world is null)
        {
            return false;
        }

        return string.Equals(world.Name, identifier, StringComparison.OrdinalIgnoreCase);
    }

    public void SetWorld(WorldInstance world)
    {
        World = world ?? throw new ArgumentNullException(nameof(world));
        world.Server = this;
    }

    public WorldInstance GetWorld() => World ?? throw new InvalidOperationException("World has not been set.");

    public void On<TSignal>(ServerEvent @event, Action<TSignal> handler) where TSignal : ISignal
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (!_signalHandlers.TryGetValue(@event, out List<Delegate>? handlers))
        {
            handlers = [];
            _signalHandlers[@event] = handlers;
        }

        handlers.Add(handler);
    }

    public void Emit(ServerEvent @event, ISignal signal)
    {
        ArgumentNullException.ThrowIfNull(signal);
        if (SignalAffinity.IsGlobalEvent(signal))
        {
            EmitHandlersInline(@event, signal);
            return;
        }

        AreaHandle? area = SignalAffinity.TryResolveArea(this, signal);
        if (area.HasValue && Properties.AreaThreadingEnabled && AreaScheduler.IsActive)
        {
            AreaScheduler.RunOnAreaThread(area.Value, () => EmitHandlersInline(@event, signal));
            return;
        }

        EmitHandlersInline(@event, signal);
    }

    public void Emit(ISignal signal) => Emit(signal.Event, signal);

    public void RunOnWorldThread(WorldInstance world, Action action)
    {
        _ = world;
        action();
    }

    public void RunOnAreaThread(AreaHandle area, Action action) =>
        AreaScheduler.RunOnAreaThread(area, action);

    public void RunOnPlayerAreaThread(Player.Player player, Action action)
    {
        if (player.Dimension is not Orion.World.Dimension dimension)
        {
            action();
            return;
        }

        int areaIndex = dimension.ShardManager.ResolveShard((int)player.Position.X >> 4, (int)player.Position.Z >> 4).AreaIndex;
        RunOnAreaThread(new AreaHandle(dimension, areaIndex), action);
    }

    public void Broadcast(DataPacket packet, params Player.Player[]? exclude)
    {
        WorldInstance world = GetWorld();
        Orion.Entity.Entity[]? exceptEntities = exclude is null ? null : [.. exclude];
        foreach (Orion.World.Dimension dimension in world.Dimensions)
        {
            BroadcastService.Broadcast(dimension, packet, exceptEntities is null ? null : new Orion.World.BroadcastOptions { Except = exceptEntities });
        }
    }

    internal void SetTps(double tps) => Tps = tps;

    void EmitHandlersInline(ServerEvent @event, ISignal signal)
    {
        if (!_signalHandlers.TryGetValue(@event, out List<Delegate>? handlers))
        {
            return;
        }

        for (int i = 0; i < handlers.Count; i++)
        {
            Delegate handler = handlers[i];
            Type? signalType = handler.Method.GetParameters().FirstOrDefault()?.ParameterType;
            if (signalType is null || !signalType.IsInstanceOfType(signal))
            {
                continue;
            }

            handler.DynamicInvoke(signal);
        }
    }
}
