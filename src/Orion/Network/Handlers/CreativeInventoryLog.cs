namespace Orion.Network.Handlers;

using Orion;
using Orion.Config;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Registry;
using Orion.Protocol.Types;
using Orion.RakNet;
using Log = Orion.Logger.Logger;

internal static class CreativeInventoryLog
{
    private static readonly HashSet<PacketId> TrackedClientPackets =
    [
        PacketId.InventoryTransaction,
        PacketId.MobEquipment,
        PacketId.ContainerClose,
        PacketId.ItemStackRequest,
        PacketId.SetLocalPlayerAsInitialized,
        PacketId.PlayerAction,
        PacketId.PlayerHotBar,
    ];

    public static void LogCatalogInit()
    {
        byte[] registryPayload = CuratedItemCatalog.GetItemRegistryPayload();
        byte[] creativePayload = CuratedItemCatalog.GetCreativeContentPayload();

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] catalog init registryBytes={0} creativeBytes={1} registryHex={2} creativeHex={3} {4}",
            registryPayload.Length,
            creativePayload.Length,
            FormatHex(registryPayload, 48),
            FormatHex(creativePayload, 64),
            DescribeCreativePayload(creativePayload));
    }

    public static void LogItemRegistrySent(string context, string player, byte[] payload)
    {
        string summary = DescribeItemRegistryPayload(payload);
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} sent ItemRegistry to {1} bytes={2} {3} hex={4}",
            context,
            player,
            payload.Length,
            summary,
            FormatHex(payload, 48));
    }

    public static void LogCreativeContentSent(string context, string player, byte[] payload)
    {
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} sent CreativeContent to {1} bytes={2} hex={3} {4}",
            context,
            player,
            payload.Length,
            FormatHex(payload, 64),
            DescribeCreativePayload(payload));
    }

    public static void LogSpawnSequence(string player, Gamemode gamemode, int startGamePlayerGameMode, int registryBytes, int creativeBytes)
    {
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] spawn sequence for {0}: StartGame -> ItemRegistry({1}b) -> AvailableActorIdentifiers -> PlayStatus(PlayerSpawn) -> CreativeContent({2}b) gamemode={3} startGame.PlayerGameMode={4}",
            player,
            registryBytes,
            creativeBytes,
            gamemode,
            startGamePlayerGameMode);
    }

    public static void LogSetGamemodeSequence(string player, Gamemode gamemode, int registryBytes, int creativeBytes)
    {
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] SetGamemode sequence for {0}: SetPlayerGameType({1}) -> UpdateAbilities -> ItemRegistry({2}b) -> CreativeContent({3}b)",
            player,
            gamemode,
            registryBytes,
            creativeBytes);
    }

    public static void LogRegistryCreativeItems()
    {
        List<string> entries = [];
        for (uint index = 0; index < 16; index++)
        {
            Item.ItemStack? item = Item.ItemType.GetCreativeItem(index);
            if (item is null)
            {
                break;
            }

            entries.Add($"#{index}:{item.Type.Identifier}(net={item.Type.NetworkId})");
        }

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] ItemRegistry creative items count={0} [{1}]",
            entries.Count,
            entries.Count == 0 ? "none" : string.Join(", ", entries));
    }

    public static void TryLogClientPacket(Server server, NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> packetBuffer)
    {
        if (!TrackedClientPackets.Contains(packetId))
        {
            return;
        }

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player))
        {
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] client->{0} bytes={1} hex={2} (no player)",
                packetId,
                packetBuffer.Length,
                FormatHex(packetBuffer, 32));
            return;
        }

        string detail = packetId switch
        {
            PacketId.InventoryTransaction => DescribeInventoryTransaction(packetBuffer),
            PacketId.ContainerClose => DescribeContainerClose(packetBuffer),
            PacketId.ItemStackRequest => $"requests=see-handler bytes={packetBuffer.Length}",
            PacketId.MobEquipment => DescribeMobEquipment(packetBuffer),
            PacketId.SetLocalPlayerAsInitialized => "client initialized",
            PacketId.PlayerAction => DescribePlayerAction(packetBuffer),
            PacketId.PlayerHotBar => $"bytes={packetBuffer.Length}",
            _ => $"bytes={packetBuffer.Length}"
        };

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} client->{1} gamemode={2} {3} hex={4}",
            player.Username,
            packetId,
            player.GetGamemode(),
            detail,
            FormatHex(packetBuffer, 32));
    }

    public static void LogItemStackResponse(string player, int requestCount, IReadOnlyList<ItemStackResponse> responses)
    {
        List<string> parts = [];
        for (int i = 0; i < responses.Count; i++)
        {
            ItemStackResponse response = responses[i];
            parts.Add($"req={response.RequestId} status={response.Status} containers={response.ContainerInfo.Count}");
        }

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} sent ItemStackResponse count={1} [{2}]",
            player,
            responses.Count,
            string.Join("; ", parts));
    }

    public static string DescribeCreativePayload(byte[] payload)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(payload, ref offset);
            CreativeContentPacket packet = new();
            packet.Deserialize(reader);

            List<string> groups = [];
            foreach (CreativeGroup group in packet.Groups)
            {
                groups.Add(
                    $"g={group.Name} cat={group.Category} icon=[{DescribeDescriptor(group.Icon)}]");
            }

            List<string> items = [];
            foreach (CreativeItem item in packet.Items)
            {
                items.Add(
                    $"idx={item.ItemIndex} grp={item.GroupIndex} inst=[{DescribeDescriptor(item.ItemInstance)}]");
            }

            return $"groups={packet.Groups.Count} [{string.Join("; ", groups)}] items={packet.Items.Count} [{string.Join("; ", items)}]";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    public static string DescribeSlot(StackRequestSlotInfo slot)
    {
        string dynamicId = slot.Container.DynamicContainerId.HasValue
            ? slot.Container.DynamicContainerId.Value.ToString()
            : "none";

        return $"container={(ContainerName)slot.Container.ContainerId}({slot.Container.ContainerId}) dynamic={dynamicId} slot={slot.Slot} stack={slot.StackNetworkId}";
    }

    private static string DescribeDescriptor(CreativeItemInstanceDescriptor descriptor)
    {
        if (descriptor.RawData is { Length: > 0 } raw)
        {
            return $"raw={raw.Length} hex={FormatHex(raw, 16)}";
        }

        int extraLen = descriptor.ExtraData is not null ? 1 : 0;
        return $"net={descriptor.NetworkId} stack={descriptor.StackSize} meta={descriptor.Metadata} block={descriptor.NetworkBlockId} extra={extraLen > 0}";
    }

    private static string DescribeItemRegistryPayload(byte[] payload)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(payload, ref offset);
            ItemRegistryPacket packet = new();
            packet.Deserialize(reader);

            List<string> items = [];
            foreach (ItemEntry entry in packet.Items)
            {
                items.Add($"{entry.Name}(id={entry.RuntimeId})");
            }

            return $"items={packet.Items.Count} [{string.Join(", ", items)}]";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    private static string DescribeInventoryTransaction(ReadOnlySpan<byte> buffer)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(buffer, ref offset);
            InventoryTransactionPacket packet = new();
            packet.Deserialize(reader);

            string dataType = packet.TransactionData?.GetType().Name ?? "null";
            return $"type={dataType} actions={packet.Actions.Count} legacySlots={packet.LegacySetItemSlots.Count}";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    private static string DescribeContainerClose(ReadOnlySpan<byte> buffer)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(buffer, ref offset);
            ContainerClosePacket packet = new();
            packet.Deserialize(reader);

            return $"windowId={packet.WindowId} containerType={packet.ContainerType} serverSide={packet.ServerSide}";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    private static string DescribeMobEquipment(ReadOnlySpan<byte> buffer)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(buffer, ref offset);
            MobEquipmentPacket packet = new();
            packet.Deserialize(reader);

            return $"slot={packet.InventorySlot} hotbarSlot={packet.HotBarSlot} window={packet.WindowId} itemNet={packet.NewItem.NetworkId}";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    private static string DescribePlayerAction(ReadOnlySpan<byte> buffer)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(buffer, ref offset);
            PlayerActionPacket packet = new();
            packet.Deserialize(reader);

            return $"action={packet.ActionType} block={packet.BlockPosition} face={packet.BlockFace}";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    private static string FormatHex(ReadOnlySpan<byte> data, int maxBytes)
    {
        int length = Math.Min(data.Length, maxBytes);
        if (length == 0)
        {
            return "";
        }

        char[] chars = new char[length * 2];
        for (int i = 0; i < length; i++)
        {
            byte value = data[i];
            chars[i * 2] = GetHexNibble(value >> 4);
            chars[(i * 2) + 1] = GetHexNibble(value & 0x0F);
        }

        string hex = new(chars);
        return data.Length > maxBytes ? $"{hex}..." : hex;
    }

    private static char GetHexNibble(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}
