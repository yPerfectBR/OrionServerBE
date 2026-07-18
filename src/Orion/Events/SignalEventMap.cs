namespace Orion.Events;

/// <summary>Maps signal CLR types to <see cref="ServerEvent"/> for typed <see cref="IEventBus"/> subscribe.</summary>
public static class SignalEventMap
{
    static readonly Dictionary<Type, ServerEvent> Map = new()
    {
        [typeof(ServerStartSignal)] = ServerEvent.ServerStart,
        [typeof(EntityHurtSignal)] = ServerEvent.EntityHurt,
        [typeof(EntitySpawnSignal)] = ServerEvent.EntitySpawn,
        [typeof(EntityDieSignal)] = ServerEvent.EntityDie,
        [typeof(PlayerChatSignal)] = ServerEvent.PlayerChat,
        [typeof(PlayerJoinSignal)] = ServerEvent.PlayerJoin,
        [typeof(PlayerSpawnSignal)] = ServerEvent.PlayerSpawn,
        [typeof(PlayerLeaveSignal)] = ServerEvent.PlayerLeave,
        [typeof(PlayerPlaceBlockSignal)] = ServerEvent.PlayerPlaceBlock,
        [typeof(PlayerBreakBlockSignal)] = ServerEvent.PlayerBreakBlock,
    };

    public static ServerEvent For<TSignal>() where TSignal : ISignal => For(typeof(TSignal));

    public static ServerEvent For(Type signalType)
    {
        ArgumentNullException.ThrowIfNull(signalType);
        if (Map.TryGetValue(signalType, out ServerEvent @event))
        {
            return @event;
        }

        throw new InvalidOperationException(
            $"No ServerEvent mapping for signal type '{signalType.FullName}'. Register it in {nameof(SignalEventMap)}.");
    }
}
