namespace Orion.Player;

using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using Orion.RakNet;
using Orion.Network;
using Orion.World.Coordinates;

/// <summary>
/// Stable server-level player session (connection and identity only).
/// Survives entity despawn during cross-worker transfers.
/// Game state (inventory, gamemode, permissions, position) lives per-world on the entity and LevelDB.
/// </summary>
public sealed class PlayerSession
{
    public required NetworkConnection Connection { get; init; }
    public required NetworkHandler Network { get; init; }

    public required string Username { get; init; }
    public required string Xuid { get; init; }
    public required Guid Uuid { get; init; }

    public DeviceOS DeviceOS { get; set; }
    public Skin Skin { get; set; } = new();

    /// <summary>World entity when spawned; null during login or transfer.</summary>
    public Player? ActiveEntity { get; set; }

    public TransferState TransferState { get; set; } = TransferState.Idle;

    public int? PendingTransferAreaIndex { get; set; }

    /// <summary>Target region while a cross-region transfer is in flight (packet routing).</summary>
    public RegionCoord? PendingTransferRegion { get; set; }

    /// <summary>Target area index while a cross-area transfer is in flight (packet routing).</summary>

    /// <summary>Pinned session worker when session threading is enabled (Phase 7b).</summary>
    public int? SessionWorkerId { get; set; }

    /// <summary>Inventory/gamemode sync deferred until the client finishes dimension/container teardown.</summary>
    public bool PendingClientWorldStateSync { get; set; }

    public ulong ClientWorldStateSyncMinTick { get; set; }

    public void Send(DataPacket packet)
    {
        SessionSendCoordinator.Send(this, packet);
    }

    public void Send(params DataPacket[] packets)
    {
        SessionSendCoordinator.Send(this, packets);
    }

    public void SendMessage(string message)
    {
        string safeMessage = string.IsNullOrEmpty(message) ? " " : message;
        Send(new TextPacket
        {
            VariantType = TextVariantType.MessageOnly,
            FilteredMessage = null,
            NeedsTranslation = false,
            Xuid = string.Empty,
            PlatformChatId = string.Empty,
            Variant = new TextVariant
            {
                Message = safeMessage,
                Parameters = [],
                Source = string.Empty,
                Type = TextType.Raw
            }
        });
    }

    public void Disconnect(string reason = "")
    {
        DisconnectPacket disconnect = new()
        {
            Reason = string.IsNullOrEmpty(reason) ? DisconnectReason.Disconnected : DisconnectReason.NetherNetSignalingSigninFailed,
            HideDisconnectionScreen = string.IsNullOrEmpty(reason),
            Message = reason,
            FilteredMessage = string.Empty
        };

        Network.SendPacket(Connection, disconnect, immediate: true);
        Connection.Disconnect();
    }
}
