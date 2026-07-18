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
    private static readonly bool Enabled = true;

    // Basalt full vanilla palette is ~1.6k creative entries / hundreds of KB.
    private const int BasaltTypicalRegistryBytesHint = 400_000;
    private const int BasaltTypicalCreativeBytesHint = 200_000;

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

    // AbortDestroyBlock/CrackBlock spam drowning creative diagnostics.
    private static readonly HashSet<PlayerActionType> IgnoredPlayerActions =
    [
        PlayerActionType.AbortDestroyBlock,
        PlayerActionType.CrackBlock,
        PlayerActionType.ContinueDestroyBlock,
        PlayerActionType.PredictDestroyBlock,
        PlayerActionType.GetUpdatedBlock,
    ];

    public static void LogCatalogInit()
    {
        if (!Enabled)
        {
            return;
        }

        byte[] registryPayload = CuratedItemCatalog.GetItemRegistryPayload();
        byte[] creativePayload = CuratedItemCatalog.GetCreativeContentPayload();

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] catalog init source={0} registryBytes={1} creativeBytes={2} registryHex={3} creativeHex={4} {5}",
            CuratedItemCatalog.Source,
            registryPayload.Length,
            creativePayload.Length,
            FormatHex(registryPayload, 48),
            FormatHex(creativePayload, 64),
            DescribeCreativePayload(creativePayload));

        LogParityDiagnosis("catalog-init", registryPayload, creativePayload);
        LogExpectedBlockIds("catalog-init");
    }

    public static void LogItemRegistrySent(string context, string player, byte[] payload)
    {
        if (!Enabled)
        {
            return;
        }

        string summary = DescribeItemRegistryPayload(payload);
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} sent ItemRegistry to {1} bytes={2} {3} hex={4}",
            context,
            player,
            payload.Length,
            summary,
            FormatHex(payload, 48));

        LogParityDiagnosis($"{context}/ItemRegistry", payload, CuratedItemCatalog.GetCreativeContentPayload());
    }

    public static void LogCreativeContentSent(string context, string player, byte[] payload)
    {
        if (!Enabled)
        {
            return;
        }

        // Tiny curated payloads: dump the entire body so we can diff against a working client capture.
        int hexBytes = payload.Length <= 512 ? payload.Length : 96;
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} sent CreativeContent to {1} bytes={2} hex={3} {4}",
            context,
            player,
            payload.Length,
            FormatHex(payload, hexBytes),
            DescribeCreativePayload(payload));

        LogCreativeLayout(context, payload);
        LogUiHints(context, player, payload);
    }

    private static void LogCreativeLayout(string context, byte[] payload)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(payload, ref offset);
            CreativeContentPacket packet = new();
            packet.Deserialize(reader);

            for (int i = 0; i < packet.Groups.Count; i++)
            {
                CreativeGroup group = packet.Groups[i];
                int itemCount = packet.Items.Count(item => item.GroupIndex == (uint)i);
                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} creative-group[{1}] cat={2}({3}) name='{4}' iconNet={5} itemsInGroup={6}",
                    context,
                    i,
                    group.Category,
                    CategoryName(group.Category),
                    group.Name,
                    group.Icon.NetworkId,
                    itemCount);
            }

            foreach (CreativeItem item in packet.Items)
            {
                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} creative-item id={1} groupIndex={2} net={3} stack={4} block={5}",
                    context,
                    item.CreativeItemNetworkId,
                    item.GroupIndex,
                    item.ItemInstance.NetworkId,
                    item.ItemInstance.StackSize,
                    item.ItemInstance.NetworkBlockId);
            }
        }
        catch (Exception exception)
        {
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] {0} creative-layout decode-error={1}",
                context,
                exception.Message);
        }
    }

    public static void LogSpawnSequence(string player, Gamemode gamemode, int startGamePlayerGameMode, int registryBytes, int creativeBytes)
    {
        if (!Enabled)
        {
            return;
        }

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] spawn sequence for {0}: StartGame -> ItemRegistry({1}b) -> AvailableActorIdentifiers -> PlayStatus(PlayerSpawn) -> CreativeContent({2}b) gamemode={3} startGame.PlayerGameMode={4}",
            player,
            registryBytes,
            creativeBytes,
            gamemode,
            startGamePlayerGameMode);
    }

    public static void LogSetGamemodeSequence(string player, Gamemode gamemode)
    {
        if (!Enabled)
        {
            return;
        }

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] SetGamemode sequence for {0}: SetPlayerGameType({1}) -> UpdateAbilities (no ItemRegistry/CreativeContent resend)",
            player,
            gamemode);
    }

    public static void LogRegistryCreativeItems()
    {
        if (!Enabled)
        {
            return;
        }

        List<string> entries = [];
        for (uint index = 1; index <= 16; index++)
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
        if (!Enabled)
        {
            return;
        }

        if (!TrackedClientPackets.Contains(packetId))
        {
            return;
        }

        ReadOnlySpan<byte> payload = SkipPacketHeader(packetBuffer);

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

        if (packetId == PacketId.PlayerAction && TryReadPlayerAction(payload, out PlayerActionType actionType)
            && IgnoredPlayerActions.Contains(actionType))
        {
            return;
        }

        string detail = packetId switch
        {
            PacketId.InventoryTransaction => DescribeInventoryTransaction(payload),
            PacketId.ContainerClose => DescribeContainerClose(payload),
            PacketId.ItemStackRequest => $"requests=see-handler bytes={packetBuffer.Length}",
            PacketId.MobEquipment => DescribeMobEquipment(payload),
            PacketId.SetLocalPlayerAsInitialized => "client initialized",
            PacketId.PlayerAction => DescribePlayerAction(payload),
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
        if (!Enabled)
        {
            return;
        }

        List<string> parts = [];
        for (int i = 0; i < responses.Count; i++)
        {
            ItemStackResponse response = responses[i];
            List<string> containers = [];
            foreach (StackResponseContainerInfo containerInfo in response.ContainerInfo)
            {
                string dynamicId = containerInfo.Container.DynamicContainerId.HasValue
                    ? containerInfo.Container.DynamicContainerId.Value.ToString()
                    : "none";

                List<string> slots = [];
                foreach (StackResponseSlotInfo slot in containerInfo.SlotInfo)
                {
                    slots.Add($"slot={slot.Slot} count={slot.Count} stackId={slot.StackNetworkId}");
                }

                containers.Add(
                    $"c={(ContainerName)containerInfo.Container.ContainerId}({containerInfo.Container.ContainerId}) dyn={dynamicId} [{string.Join(", ", slots)}]");
            }

            parts.Add(
                $"req={response.RequestId} status={response.Status} containers={response.ContainerInfo.Count} [{string.Join("; ", containers)}]");
        }

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} sent ItemStackResponse count={1} [{2}]",
            player,
            responses.Count,
            string.Join("; ", parts));
    }

    public static void LogGive(
        string player,
        string item,
        int amount,
        int given,
        int networkId,
        int blockRuntimeId,
        int stackNetworkId)
    {
        if (!Enabled)
        {
            return;
        }

        Log.Info(
            LogCategory.Orion,
            "[Inv] give player={0} item={1} amount={2} given={3} netId={4} blockRuntime={5} stackId={6}",
            player,
            item,
            amount,
            given,
            networkId,
            blockRuntimeId,
            stackNetworkId);
    }

    public static void LogItemStackAction(string player, string phase, string detail)
    {
        if (!Enabled)
        {
            return;
        }

        Log.Info(LogCategory.Orion, "[Inv] isr {0} player={1} {2}", phase, player, detail);
    }

    public static string DescribeCreativePayload(byte[] payload)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(payload, ref offset);
            CreativeContentPacket packet = new();
            packet.Deserialize(reader);

            int[] categoryCounts = new int[5];
            List<string> groups = [];
            foreach (CreativeGroup group in packet.Groups)
            {
                if (group.Category is >= 0 and <= 4)
                {
                    categoryCounts[group.Category]++;
                }

                groups.Add(
                    $"g={group.Name} cat={group.Category}({CategoryName(group.Category)}) icon=[{DescribeDescriptor(group.Icon)}]");
            }

            List<string> items = [];
            Dictionary<uint, int> itemsPerGroup = [];
            int shown = 0;
            foreach (CreativeItem item in packet.Items)
            {
                itemsPerGroup[item.GroupIndex] = itemsPerGroup.GetValueOrDefault(item.GroupIndex) + 1;
                if (shown < 8)
                {
                    items.Add(
                        $"id={item.CreativeItemNetworkId} grp={item.GroupIndex} inst=[{DescribeDescriptor(item.ItemInstance)}]");
                    shown++;
                }
            }

            if (packet.Items.Count > shown)
            {
                items.Add($"...+{packet.Items.Count - shown} more");
            }

            string categorySummary =
                $"construction(groups)={categoryCounts[1]} nature={categoryCounts[2]} equipment={categoryCounts[3]} items={categoryCounts[4]}";

            List<string> shownGroups = groups.Count <= 12
                ? groups
                : [.. groups.Take(8), $"...+{groups.Count - 8} more groups"];

            return $"groups={packet.Groups.Count} [{string.Join("; ", shownGroups)}] items={packet.Items.Count} [{string.Join("; ", items)}] {categorySummary}";
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

    private static void LogParityDiagnosis(string context, byte[] registryPayload, byte[] creativePayload)
    {
        List<string> warnings = [];

        if (registryPayload.Length < 1_000)
        {
            warnings.Add(
                $"ItemRegistry tiny ({registryPayload.Length}b vs Basalt ~{BasaltTypicalRegistryBytesHint}b+) — client replaces vanilla palette with this set");
        }

        if (creativePayload.Length < 1_000)
        {
            warnings.Add(
                $"CreativeContent tiny ({creativePayload.Length}b vs Basalt ~{BasaltTypicalCreativeBytesHint}b+) — only curated entries exist");
        }

        string source = CuratedItemCatalog.Source;
        if (!source.StartsWith("vanilla", StringComparison.Ordinal))
        {
            warnings.Add($"catalog source is '{source}' (expected vanilla item_types.json + orion creative)");
        }

        try
        {
            int offset = 0;
            BinaryReader reader = new(creativePayload, ref offset);
            CreativeContentPacket packet = new();
            packet.Deserialize(reader);

            bool hasConstruction = packet.Groups.Exists(group => group.Category == 1);
            bool hasNature = packet.Groups.Exists(group => group.Category == 2);
            if (!hasConstruction && hasNature)
            {
                warnings.Add(
                    "all creative groups are Nature(cat=2) — Construction tab (default) is empty; check Nature tab");
            }

            if (packet.Groups.Exists(group =>
                    string.Equals(group.Name, "itemGroup.name.grass", StringComparison.Ordinal)
                    && packet.Items.Count <= 16))
            {
                warnings.Add(
                    "group itemGroup.name.grass used on a tiny curated catalog — Basalt puts grass_block/dirt/bedrock ungrouped in Nature");
            }

            bool missingAir = true;
            int emptyNbt = 0;
            offset = 0;
            reader = new BinaryReader(registryPayload, ref offset);
            ItemRegistryPacket registry = new();
            registry.Deserialize(reader);
            foreach (ItemEntry entry in registry.Items)
            {
                if (string.Equals(entry.Name, "minecraft:air", StringComparison.Ordinal) || entry.RuntimeId == 0)
                {
                    missingAir = false;
                }

                if (entry.Data.Values.Count == 0)
                {
                    emptyNbt++;
                }
            }

            if (missingAir)
            {
                warnings.Add("ItemRegistry has no minecraft:air (Basalt includes air netId=0)");
            }

            if (emptyNbt == registry.Items.Count && registry.Items.Count > 0)
            {
                warnings.Add($"all {emptyNbt} ItemRegistry entries have empty component NBT");
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"parity-decode-error={exception.Message}");
        }

        if (warnings.Count == 0)
        {
            Log.Info(LogCategory.Orion, "[CreativeInv] {0} basalt-parity: ok", context);
            return;
        }

        Log.Warn(
            LogCategory.Orion,
            "[CreativeInv] {0} basalt-parity warnings ({1}): {2}",
            context,
            warnings.Count,
            string.Join(" | ", warnings));
    }

    private static void LogUiHints(string context, string player, byte[] creativePayload)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(creativePayload, ref offset);
            CreativeContentPacket packet = new();
            packet.Deserialize(reader);

            bool hasConstruction = packet.Groups.Exists(group => group.Category == 1);
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] {0} UI hint for {1}: creativeItems={2} groups={3} constructionTab={4} natureTab={5} — if menu looks empty, open Nature; CraftCreative ISR never arriving means client did not pick an entry",
                context,
                player,
                packet.Items.Count,
                packet.Groups.Count,
                hasConstruction ? "has-groups" : "EMPTY",
                packet.Groups.Exists(group => group.Category == 2) ? "has-groups" : "EMPTY");
        }
        catch (Exception exception)
        {
            Log.Warn(LogCategory.Orion, "[CreativeInv] {0} UI hint failed: {1}", context, exception.Message);
        }
    }

    private static void LogExpectedBlockIds(string context)
    {
        (string Id, int Net, int Block)[] expected =
        [
            ("minecraft:grass_block", 2, -567203660),
            ("minecraft:dirt", 3, -2108756090),
            ("minecraft:bedrock", 7, -173245189),
        ];

        List<string> lines = [];
        foreach ((string id, int net, int block) in expected)
        {
            if (!CuratedItemCatalog.TryGetByIdentifier(id, out CuratedItem item))
            {
                lines.Add($"{id}: MISSING");
                continue;
            }

            bool netOk = item.NetworkId == net;
            bool blockOk = item.BlockStateHash == block;
            int creativeId = FindCreativeId(id);
            lines.Add(
                $"{id}: net={item.NetworkId}{(netOk ? "" : $"!=expected{net}")} block={item.BlockStateHash}{(blockOk ? "" : $"!=expected{block}")} creativeId={creativeId}");
        }

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} expected-block-ids [{1}]",
            context,
            string.Join("; ", lines));
    }

    private static int FindCreativeId(string identifier)
    {
        for (int i = 1; i <= 64; i++)
        {
            if (CuratedItemCatalog.TryGetCreativeMenuItem(i, out CuratedItem item)
                && string.Equals(item.Identifier, identifier, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string CategoryName(int category) => category switch
    {
        1 => "Construction",
        2 => "Nature",
        3 => "Equipment",
        4 => "Items",
        _ => $"Unknown({category})"
    };

    private static string DescribeDescriptor(CreativeItemInstanceDescriptor descriptor)
    {
        if (descriptor.RawData is { Length: > 0 } raw)
        {
            return $"raw={raw.Length} hex={FormatHex(raw, 16)}";
        }

        bool hasExtra = descriptor.ExtraData is not null;
        return $"net={descriptor.NetworkId} stack={descriptor.StackSize} meta={descriptor.Metadata} block={descriptor.NetworkBlockId} extra={hasExtra}";
    }

    private static string FormatDescriptorHex(CreativeItemInstanceDescriptor descriptor, int maxBytes)
    {
        try
        {
            byte[] buffer = new byte[256];
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);
            descriptor.Write(writer);
            return FormatHex(writer.GetProcessedBytes(), maxBytes);
        }
        catch
        {
            return "?";
        }
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
            int shown = 0;
            foreach (ItemEntry entry in packet.Items)
            {
                if (shown++ >= 12)
                {
                    items.Add($"...+{packet.Items.Count - 12} more");
                    break;
                }

                items.Add(
                    $"{entry.Name}(id={entry.RuntimeId} comp={entry.ComponentBased} ver={entry.Version} nbtKeys={entry.Data.Values.Count})");
            }

            return $"items={packet.Items.Count} [{string.Join(", ", items)}]";
        }
        catch (Exception exception)
        {
            return $"decode-error={exception.Message}";
        }
    }

    private static bool TryReadPlayerAction(ReadOnlySpan<byte> buffer, out PlayerActionType actionType)
    {
        actionType = PlayerActionType.Unknown;
        try
        {
            int offset = 0;
            BinaryReader reader = new(buffer, ref offset);
            PlayerActionPacket packet = new();
            packet.Deserialize(reader);
            actionType = packet.ActionType;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ReadOnlySpan<byte> SkipPacketHeader(ReadOnlySpan<byte> packetBuffer)
    {
        try
        {
            int offset = 0;
            BinaryReader reader = new(packetBuffer, ref offset);
            _ = reader.ReadVarUInt();
            return packetBuffer[offset..];
        }
        catch
        {
            return packetBuffer;
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

            return $"slot={packet.InventorySlot} hotbarSlot={packet.HotBarSlot} window={packet.WindowId} itemNet={packet.NewItem.NetworkId} count={packet.NewItem.Count} block={packet.NewItem.BlockRuntimeId}";
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
