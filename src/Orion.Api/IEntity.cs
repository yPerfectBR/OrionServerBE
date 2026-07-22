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
