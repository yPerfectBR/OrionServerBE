using Orion.Api.Containers;
using Orion.Api.Items;
using Orion.Api.Math;
using Orion.Api.Network;

namespace Orion.Api;

public interface IEntity
{
    long UniqueId { get; }
    ulong RuntimeId { get; }
    string TypeIdentifier { get; }
    IDimension? Dimension { get; }
    Vec3f Position { get; }
    Vec3f Velocity { get; set; }
    bool IsAlive { get; }
    bool IsSprinting { get; }
    bool IsSwimming { get; }
    bool IsPlayer();
    T? GetTrait<T>() where T : class;
    void NotifyContainerUpdate(Orion.Api.Containers.IContainer container);

    /// <summary>Upserts a Bedrock attribute by protocol name (e.g. <c>minecraft:health</c>).</summary>
    void SetAttribute(string name, float min, float max, float current, float defaultValue);

    /// <summary>Reads a Bedrock attribute by protocol name.</summary>
    bool TryGetAttribute(
        string name,
        out float min,
        out float max,
        out float current,
        out float defaultValue);

    /// <summary>Flushes dirty attributes to the client when this entity is a player.</summary>
    void SyncAttributes();

    /// <summary>Kills the entity (marks dead + pending despawn).</summary>
    void Kill(IEntity? killer = null, int? damageCause = null);

    /// <summary>Reads a Bedrock actor flag by name (e.g. <c>Breathing</c>, <c>Sprinting</c>).</summary>
    bool GetActorFlag(string flag);

    /// <summary>Upserts a Bedrock actor flag by name (e.g. <c>Breathing</c>, <c>Sprinting</c>).</summary>
    void SetActorFlag(string flag, bool value);

    /// <summary>Whether the entity currently has the given status effect (e.g. <c>WaterBreathing</c>).</summary>
    bool HasEffect(string effectName);

    /// <summary>
    /// Authoritative world-position write (e.g. server-side movement/physics plugins).
    /// Default no-op unless the host overrides it.
    /// </summary>
    void SetPosition(Vec3f position) { }

    /// <summary>
    /// Notifies the entity that a physics/movement tick completed, driving host-side
    /// follow-up behavior (e.g. item pickup/merge). No-op unless the host overrides it.
    /// </summary>
    void NotifyPhysicsTick(bool grounded) { }

    /// <summary>
    /// Whether the entity is queued for despawn (between death and removal from the dimension).
    /// </summary>
    bool IsPendingDespawn => false;

    /// <summary>
    /// Sends spawn packets so <paramref name="observer"/> can see this entity client-side.
    /// </summary>
    void PresentTo(IPlayer observer) { }
}

public interface IPlayer : IEntity
{
    string Username { get; }
    string Xuid { get; }
    Guid Uuid { get; }
    bool IsOnline { get; }
    bool Spawned { get; }
    bool IsOperator { get; }
    bool IsFlying { get; }
    float Yaw { get; }
    float Pitch { get; }
    Gamemode Gamemode { get; }
    void SetGamemode(Gamemode gamemode);
    void SendMessage(string message);
    void Disconnect(string reason = "");
    void Teleport(Vec3f position, IDimension? dimension = null, bool forceDimensionChange = false);
    void Send(params IOutboundPacket[] packets);
    void SetHud(HudVisibility visibility, params HudElement[] elements);
    bool DropItem(IItemStack item);
    void SyncInventoryToClient();
    IReadOnlyDictionary<int, IContainer> OpenedContainers { get; }
    void RegisterOpenContainer(int windowId, IContainer container);
    bool TryGetOpenContainer(int windowId, out IContainer? container);
    void UnregisterOpenContainer(int windowId);
    void FlushPendingClientSync(bool force = false);
    bool HasPermission(string permission);
}
