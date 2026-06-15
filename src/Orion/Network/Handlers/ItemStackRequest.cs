namespace Orion.Network.Handlers;

using Orion.Containers;
using Orion;
using Orion.Config;
using Orion.Entity.Traits;
using Orion.Item;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Registry;
using Orion.Protocol.Types;
using Orion.RakNet;
using Orion.Player.Traits;
using Log = Orion.Logger.Logger;


public static class ItemStackRequest
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        ItemStackRequestPacket packet = new();
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ItemStackRequestPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!SessionLookup.TryGetPlayer(server, connection, out global::Orion.Player.Player? player) || packet.Requests.Count == 0)
        {
            return;
        }

        if (player.Gamemode == Gamemode.Creative)
        {
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] {0} packet requests={1}",
                player.Username,
                packet.Requests.Count);
        }

        List<ItemStackResponse> responses = new(packet.Requests.Count);

        foreach (Protocol.Types.ItemStackRequest request in packet.Requests)
        {
            try
            {
                responses.Add(ProcessRequest(player, request));
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ItemStackRequest exception: request: {request.RequestId} {exception}");

                responses.Add(new ItemStackResponse
                {
                    Status = ItemStackResponseStatus.Error,
                    RequestId = request.RequestId,
                    ContainerInfo = []
                });
            }
        }

        ItemStackResponsePacket responsePacket = new()
        {
            Responses = responses
        };

        if (player.Session is not null)
        {
            player.Session.Send(responsePacket);
        }
        else
        {
            server.Network.SendPacket(connection, responsePacket);
        }

        if (player.Gamemode == Gamemode.Creative)
        {
            CreativeInventoryLog.LogItemStackResponse(player.Username, responses.Count, responses);
            if (responses.All(response => response.Status == ItemStackResponseStatus.Ok))
            {
                SyncCreativeContainersToClient(player);
            }
        }
    }

    public static ItemStackResponse Process(global::Orion.Player.Player player, Protocol.Types.ItemStackRequest request) =>
        ProcessRequest(player, request);

    private static ItemStackResponse ProcessRequest(global::Orion.Player.Player player, Protocol.Types.ItemStackRequest request)
    {
        Dictionary<string, StackResponseContainerInfo> changedContainers = [];
        bool logCreative = player.Gamemode == Gamemode.Creative;
        bool suppressNetworkSync = logCreative;

        if (suppressNetworkSync)
        {
            SetSuppressNetworkSync(player, true);
        }

        try
        {
            if (logCreative)
            {
                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} request={1} actions={2}",
                    player.Username,
                    request.RequestId,
                    request.Actions.Count);
            }

            foreach (IStackRequestAction action in request.Actions)
            {
                if (logCreative)
                {
                    Log.Info(
                        LogCategory.Orion,
                        "[CreativeInv] {0} request={1} action={2}",
                        player.Username,
                        request.RequestId,
                        DescribeAction(action));
                }

                ItemStackResponseStatus status = action switch
            {
                TransferStackRequestAction transfer => TransferItem(player, transfer, changedContainers),
                SwapStackRequestAction swap => SwapItems(player, swap, changedContainers),
                DropStackRequestAction drop => RemoveDroppedItem(player, drop, changedContainers),
                DestroyStackRequestAction destroy => RemoveDestroyedItem(player, destroy, changedContainers),
                CraftCreativeStackRequestAction craftCreative => CreateCreativeItem(player, craftCreative, changedContainers),

                EmptyStackRequestAction => ItemStackResponseStatus.Ok,
                CraftResultsDeprecatedStackRequestAction => ItemStackResponseStatus.Ok,

                _ => ItemStackResponseStatus.InvalidRequestActionType
            };

                if (status == ItemStackResponseStatus.Ok)
                {
                    continue;
                }

                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} request={1} FAILED status={2} action={3}",
                    player.Username,
                    request.RequestId,
                    status,
                    DescribeAction(action));
                Console.WriteLine($"ItemStackRequest failed: request: {request.RequestId} status={status} action={DescribeAction(action)}");
                foreach (Container container in player.openedContainers.Values.Distinct())
                {
                    container.Update();
                }

                player.GetTrait<PlayerCursorTrait>()?.Container.UpdateSlot(0);
                player.GetTrait<PlayerCraftingOutputTrait>()?.Container.UpdateSlot(0);

                return new ItemStackResponse
                {
                    Status = status,
                    RequestId = request.RequestId,
                    ContainerInfo = []
                };
            }

            if (logCreative)
            {
                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} request={1} OK changedContainers={2}",
                    player.Username,
                    request.RequestId,
                    changedContainers.Count);
            }

            return new ItemStackResponse
            {
                Status = ItemStackResponseStatus.Ok,
                RequestId = request.RequestId,
                ContainerInfo = changedContainers.Count > 0
                    ? [.. changedContainers.Values]
                    : []
            };
        }
        finally
        {
            if (suppressNetworkSync)
            {
                SetSuppressNetworkSync(player, false);
            }
        }
    }

    private static ItemStackResponseStatus TransferItem(
        global::Orion.Player.Player player,
        TransferStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveCreativeTransferSource(player, action.Source, out Container sourceContainer, out int sourceSlot))
        {
            if (player.Gamemode == Gamemode.Creative)
            {
                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} transfer INVALID src {1}",
                    player.Username,
                    CreativeInventoryLog.DescribeSlot(action.Source));
            }

            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        bool destinationResolved = TryResolveSlot(player, action.Destination, out Container destinationContainer, out int destinationSlot);

        if (!destinationResolved)
        {
            if (player.Gamemode == Gamemode.Creative)
            {
                Log.Info(
                    LogCategory.Orion,
                    "[CreativeInv] {0} transfer INVALID dst {1} fromOutput={2}",
                    player.Username,
                    CreativeInventoryLog.DescribeSlot(action.Destination),
                    IsCreativeOutputContainer(player, sourceContainer));
            }

            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (player.Gamemode == Gamemode.Creative)
        {
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] {0} transfer src={1} slot={2} item={3} dst={4} dstSlot={5} type={6} count={7}",
                player.Username,
                sourceContainer.GetType().Name,
                sourceSlot,
                sourceContainer.GetItem(sourceSlot)?.Type.Identifier ?? "null",
                destinationContainer.GetType().Name,
                destinationSlot,
                action.ActionType,
                action.Count);
        }

        if (sourceSlot < 0 || sourceSlot >= sourceContainer.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        ItemStack? sourceItem = sourceContainer.GetItem(sourceSlot);
        if (sourceItem is null)
        {
            return ItemStackResponseStatus.FailedToMatchExpectedSlotConsumedItem;
        }

        int amount = ResolveTransferAmount(player, action, sourceContainer, sourceItem);

        if (destinationSlot < 0)
        {
            destinationSlot = ResolveDestinationSlot(destinationContainer, sourceItem, -1);
        }
        else if (destinationSlot >= destinationContainer.GetSize())
        {
            destinationSlot = ResolveDestinationSlot(destinationContainer, sourceItem, -1);
        }
        else
        {
            ItemStack? existing = destinationContainer.GetItem(destinationSlot);
            if (existing is not null && !existing.CanStackWith(sourceItem))
            {
                int alternate = ResolveDestinationSlot(destinationContainer, sourceItem, destinationSlot);
                if (alternate >= 0)
                {
                    destinationSlot = alternate;
                }
            }
        }

        if (destinationSlot < 0 || destinationSlot >= destinationContainer.GetSize())
        {
            return ItemStackResponseStatus.CannotPlaceItem;
        }

        ItemStack? movedItem = sourceContainer.TakeItem(sourceSlot, amount);
        if (movedItem is null || movedItem.Type == ItemType.Air || movedItem.StackSize == 0)
        {
            return ItemStackResponseStatus.CannotRemoveItem;
        }

        ItemStack? destinationItem = destinationContainer.GetItem(destinationSlot);
        if (destinationItem is null)
        {
            destinationContainer.SetItem(destinationSlot, movedItem);
        }
        else if (destinationItem.CanStackWith(movedItem))
        {
            int space = destinationItem.Type.MaxStackSize - destinationItem.StackSize;
            if (space <= 0)
            {
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            int merge = Math.Min(space, movedItem.StackSize);
            destinationItem.IncrementStack((ushort)merge);
            movedItem.DecrementStack((ushort)merge);
            if (movedItem.StackSize > 0)
            {
                int overflow = ResolveDestinationSlot(destinationContainer, movedItem, -1);
                if (overflow < 0)
                {
                    sourceContainer.SetItem(sourceSlot, movedItem);
                    return ItemStackResponseStatus.CannotPlaceItem;
                }

                destinationContainer.SetItem(overflow, movedItem);
            }

            destinationContainer.UpdateSlot(destinationSlot);
        }
        else if (destinationContainer.GetSize() == 1)
        {
            ItemStack? previousDestination = destinationContainer.GetItem(destinationSlot);
            destinationContainer.SetItem(destinationSlot, movedItem);
            if (previousDestination is null)
            {
                sourceContainer.ClearSlot(sourceSlot);
            }
            else
            {
                sourceContainer.SetItem(sourceSlot, previousDestination);
            }
        }
        else
        {
            int alternate = ResolveDestinationSlot(destinationContainer, movedItem, -1);
            if (alternate < 0)
            {
                sourceContainer.SetItem(sourceSlot, movedItem);
                return ItemStackResponseStatus.CannotPlaceItem;
            }

            destinationContainer.SetItem(alternate, movedItem);
        }

        AddChangedSlot(changedContainers, action.Source.Container, sourceContainer, action.Source.Slot, sourceSlot);
        AddChangedSlot(changedContainers, action.Destination.Container, destinationContainer, action.Destination.Slot, destinationSlot);

        return ItemStackResponseStatus.Ok;
    }

    private static ushort ResolveCreativeStackCount(ItemStack item, byte numberOfCrafts)
    {
        ushort maxStack = (ushort)Math.Max(1, item.Type.MaxStackSize);
        if (numberOfCrafts == 0)
        {
            return maxStack;
        }

        return (ushort)Math.Min(numberOfCrafts, maxStack);
    }

    private static bool IsCreativeOutputContainer(global::Orion.Player.Player player, Container container)
    {
        return ReferenceEquals(player.GetTrait<PlayerCraftingOutputTrait>()?.Container, container);
    }

    /// <summary>
    /// Resolves creative pick destinations from the client's container encoding.
    /// Shift: AnvilMaterial + dynamic → inventory. Normal: AnvilInput + dynamic or Cursor → cursor.
    /// </summary>
    private static bool TryResolveCreativePickDestination(
        global::Orion.Player.Player player,
        StackRequestSlotInfo destination,
        out Container container,
        out int slot)
    {
        container = null!;
        slot = -1;

        PlayerCursorTrait? cursorTrait = player.GetTrait<PlayerCursorTrait>();
        EntityInventoryTrait? inventoryTrait = player.GetTrait<EntityInventoryTrait>();
        Container? cursor = cursorTrait?.Container;
        Container? inventory = inventoryTrait?.Container;

        FullContainerName containerName = destination.Container;
        ContainerName name = (ContainerName)containerName.ContainerId;
        int uiSlot = destination.Slot;

        if (name == ContainerName.Cursor || uiSlot == (byte)ContainerName.Cursor)
        {
            if (cursor is null)
            {
                return false;
            }

            container = cursor;
            slot = 0;
            return true;
        }

        if (name == ContainerName.HotbarAndInventory || uiSlot == (byte)ContainerName.HotbarAndInventory
            || name is ContainerName.Hotbar or ContainerName.Inventory)
        {
            if (inventory is null)
            {
                return false;
            }

            container = inventory;
            slot = -1;
            return true;
        }

        if (!containerName.DynamicContainerId.HasValue)
        {
            return false;
        }

        if (name == ContainerName.AnvilMaterial)
        {
            if (inventory is null)
            {
                return false;
            }

            container = inventory;
            slot = -1;
            return true;
        }

        if (name == ContainerName.AnvilInput)
        {
            if (cursor is null)
            {
                return false;
            }

            container = cursor;
            slot = 0;
            return true;
        }

        return false;
    }

    private static int ResolveTransferAmount(
        global::Orion.Player.Player player,
        TransferStackRequestAction action,
        Container sourceContainer,
        ItemStack sourceItem)
    {
        if (player.Gamemode == Gamemode.Creative && IsCreativeOutputContainer(player, sourceContainer))
        {
            int requested = action.Count == 0
                ? sourceItem.Type.MaxStackSize
                : action.Count;

            requested = Math.Min(requested, sourceItem.Type.MaxStackSize);

            if (requested <= 0)
            {
                requested = sourceItem.Type.MaxStackSize;
            }

            if (sourceItem.StackSize < requested)
            {
                sourceItem.SetStackSize((ushort)requested);
            }

            return Math.Max(1, Math.Min(requested, sourceItem.StackSize));
        }

        if (action.Count == 0)
        {
            return Math.Max(1, (int)sourceItem.StackSize);
        }

        return Math.Min(Math.Max(1, (int)action.Count), sourceItem.StackSize);
    }

    private static int ResolveDestinationSlot(Container container, ItemStack sourceItem, int preferredSlot)
    {
        if (preferredSlot >= 0 && preferredSlot < container.GetSize())
        {
            ItemStack? preferred = container.GetItem(preferredSlot);
            if (preferred is null)
            {
                return preferredSlot;
            }

            if (preferred.CanStackWith(sourceItem) && preferred.StackSize < preferred.Type.MaxStackSize)
            {
                return preferredSlot;
            }
        }

        for (int i = 0; i < container.GetSize(); i++)
        {
            ItemStack? item = container.GetItem(i);
            if (item is null)
            {
                continue;
            }

            if (item.CanStackWith(sourceItem) && item.StackSize < item.Type.MaxStackSize)
            {
                return i;
            }
        }

        for (int i = 0; i < container.GetSize(); i++)
        {
            if (container.GetItem(i) is null)
            {
                return i;
            }
        }

        return -1;
    }

    private static ItemStackResponseStatus CreateCreativeItem(
        global::Orion.Player.Player player,
        CraftCreativeStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (player.Gamemode != Gamemode.Creative)
        {
            return ItemStackResponseStatus.PlayerNotInCreativeMode;
        }

        PlayerCraftingOutputTrait? craftingOutput = player.GetTrait<PlayerCraftingOutputTrait>();
        Container? output = craftingOutput?.Container;
        if (output is null)
        {
            return ItemStackResponseStatus.MissingCreatedOutputContainer;
        }

        ItemStack? item = ResolveCreativeCraftItem(action.CreativeItemNetworkId);
        if (item is null)
        {
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] {0} craftCreative FAILED index={1} (ResolveCreativeCraftItem returned null)",
                player.Username,
                action.CreativeItemNetworkId);
            return ItemStackResponseStatus.FailedToCraftCreative;
        }

        output.Clear();
        item.SetStackSize(ResolveCreativeStackCount(item, action.NumberOfCrafts));
        output.SetItem(0, item);

        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] {0} craftCreative index={1} item={2}x{3} output={4}",
            player.Username,
            action.CreativeItemNetworkId,
            item.Type.Identifier,
            item.StackSize,
            output.GetType().Name);

        FullContainerName containerName = new()
        {
            ContainerId = (byte)ContainerName.CreativeOutput
        };

        AddChangedSlot(changedContainers, containerName, output, 0, 0);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus SwapItems(
        global::Orion.Player.Player player,
        SwapStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container sourceContainer, out int sourceSlot) ||
            !TryResolveSlot(player, action.Destination, out Container destinationContainer, out int destinationSlot))
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (sourceSlot < 0 || sourceSlot >= sourceContainer.GetSize() ||
            destinationSlot < 0 || destinationSlot >= destinationContainer.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        sourceContainer.SwapItems(sourceSlot, destinationSlot, destinationContainer);

        AddChangedSlot(changedContainers, action.Source.Container, sourceContainer, action.Source.Slot, sourceSlot);
        AddChangedSlot(changedContainers, action.Destination.Container, destinationContainer, action.Destination.Slot, destinationSlot);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus RemoveDroppedItem(
        global::Orion.Player.Player player,
        DropStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot))
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (slot < 0 || slot >= container.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removedItem = container.TakeItem(slot, amount);

        if (removedItem is null)
        {
            return ItemStackResponseStatus.CannotDropItem;
        }

        _ = player.DropItem(removedItem);

        AddChangedSlot(changedContainers, action.Source.Container, container, action.Source.Slot, slot);

        return ItemStackResponseStatus.Ok;
    }

    private static ItemStackResponseStatus RemoveDestroyedItem(
        global::Orion.Player.Player player,
        DestroyStackRequestAction action,
        Dictionary<string, StackResponseContainerInfo> changedContainers)
    {
        if (!TryResolveSlot(player, action.Source, out Container container, out int slot))
        {
            return ItemStackResponseStatus.InvalidSourceContainer;
        }

        if (player.Gamemode == Gamemode.Creative && IsCreativeProtectedContainer(player, container))
        {
            Log.Info(
                LogCategory.Orion,
                "[CreativeInv] {0} destroy IGNORED protected={1} slot={2}",
                player.Username,
                container.GetType().Name,
                slot);
            return ItemStackResponseStatus.Ok;
        }

        if (slot < 0 || slot >= container.GetSize())
        {
            return ItemStackResponseStatus.FailedToValidateSrcSlot;
        }

        int amount = Math.Max(1, (int)action.Count);
        ItemStack? removedItem = container.TakeItem(slot, amount);

        if (removedItem is null)
        {
            return ItemStackResponseStatus.CannotDestroyItem;
        }

        AddChangedSlot(changedContainers, action.Source.Container, container, action.Source.Slot, slot);

        return ItemStackResponseStatus.Ok;
    }

    private static void SetSuppressNetworkSync(global::Orion.Player.Player player, bool suppress)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is not null)
        {
            inventory.Container.SuppressNetworkSync = suppress;
        }

        PlayerCursorTrait? cursor = player.GetTrait<PlayerCursorTrait>();
        if (cursor is not null)
        {
            cursor.Container.SuppressNetworkSync = suppress;
        }

        PlayerCraftingOutputTrait? craftingOutput = player.GetTrait<PlayerCraftingOutputTrait>();
        if (craftingOutput is not null)
        {
            craftingOutput.Container.SuppressNetworkSync = suppress;
        }
    }

    private static void SyncCreativeContainersToClient(global::Orion.Player.Player player)
    {
        player.SyncInventoryToClient();

        PlayerCursorTrait? cursor = player.GetTrait<PlayerCursorTrait>();
        cursor?.Container.UpdateSlot(0);

        PlayerCraftingOutputTrait? craftingOutput = player.GetTrait<PlayerCraftingOutputTrait>();
        craftingOutput?.Container.UpdateSlot(0);
    }

    private static bool IsCreativeProtectedContainer(global::Orion.Player.Player player, Container container)
    {
        if (player.Gamemode != Gamemode.Creative)
        {
            return false;
        }

        PlayerCraftingOutputTrait? craftingOutput = player.GetTrait<PlayerCraftingOutputTrait>();
        return craftingOutput is not null && ReferenceEquals(craftingOutput.Container, container);
    }

    private static void AddChangedSlot(
        Dictionary<string, StackResponseContainerInfo> changedContainers,
        FullContainerName containerName,
        Container container,
        int responseSlot,
        int storageSlot)
    {
        string containerKey = containerName.DynamicContainerId.HasValue
            ? $"{containerName.ContainerId}:{containerName.DynamicContainerId.Value}"
            : containerName.ContainerId.ToString();

        if (!changedContainers.TryGetValue(containerKey, out StackResponseContainerInfo? containerInfo))
        {
            containerInfo = new StackResponseContainerInfo
            {
                Container = new FullContainerName
                {
                    ContainerId = containerName.ContainerId,
                    DynamicContainerId = containerName.DynamicContainerId
                },
                SlotInfo = []
            };

            changedContainers[containerKey] = containerInfo;
        }

        ItemStack? item = container.GetItem(storageSlot);

        containerInfo.SlotInfo.RemoveAll(slot => slot.Slot == responseSlot);
        containerInfo.SlotInfo.Add(new StackResponseSlotInfo
        {
            Slot = (byte)responseSlot,
            HotbarSlot = (byte)responseSlot,
            Count = (byte)(item?.StackSize ?? 0),
            StackNetworkId = item?.NetworkStackId ?? 0,
            CustomName = string.Empty,
            FilteredCustomName = string.Empty,
            DurabilityCorrection = 0
        });
    }

    private static bool TryResolveSlot(global::Orion.Player.Player player, StackRequestSlotInfo requestSlot, out Container container, out int slot)
    {
        container = null!;
        slot = -1;

        FullContainerName containerName = requestSlot.Container;
        ContainerName name = (ContainerName)containerName.ContainerId;
        int uiSlot = requestSlot.Slot;

        PlayerCursorTrait? cursor = player.GetTrait<PlayerCursorTrait>();
        PlayerCraftingOutputTrait? craftingOutput = player.GetTrait<PlayerCraftingOutputTrait>();

        if (uiSlot == (byte)ContainerName.Cursor)
        {
            container = cursor?.Container ?? player.GetContainer(containerName);
            slot = 0;
            return container is not null;
        }

        if (uiSlot is (byte)ContainerName.CreativeOutput or (byte)ContainerName.CraftingOutput)
        {
            container = craftingOutput?.Container ?? player.GetContainer(containerName);
            slot = 0;
            return container is not null;
        }

        container = player.GetContainer(containerName);
        if (container is null)
        {
            return false;
        }

        if (uiSlot == (byte)ContainerName.HotbarAndInventory)
        {
            slot = -1;
            return true;
        }

        if (craftingOutput?.Container is not null && ReferenceEquals(container, craftingOutput.Container))
        {
            slot = 0;
            return true;
        }

        if (uiSlot >= container.GetSize())
        {
            return false;
        }

        if (name is ContainerName.AnvilInput or ContainerName.AnvilMaterial or ContainerName.CreativeOutput or ContainerName.CraftingOutput)
        {
            slot = uiSlot;
            if (slot < 0 || slot >= container.GetSize())
            {
                slot = 0;
            }

            return true;
        }

        slot = MapInventorySlot(containerName, uiSlot);
        return slot >= 0 || slot == -1;
    }

    private static int MapInventorySlot(FullContainerName containerName, int uiSlot)
    {
        if (containerName.ContainerId is (byte)ContainerName.Armor
            or (byte)ContainerName.HotbarAndInventory
            or (byte)ContainerName.Hotbar
            or (byte)ContainerName.Inventory
            or (byte)ContainerId.Armor
            or (byte)ContainerId.Inventory
            or (byte)ContainerId.Hotbar
            or (byte)ContainerId.FixedInventory
            or (byte)ContainerId.Offhand)
        {
            if (uiSlot is >= 36 and <= 44)
            {
                return uiSlot - 36;
            }
        }

        return uiSlot;
    }

    private static bool TryResolveCreativeTransferSource(
        global::Orion.Player.Player player,
        StackRequestSlotInfo source,
        out Container container,
        out int slot)
    {
        container = null!;
        slot = -1;

        if (TryResolveSlot(player, source, out container, out slot) &&
            slot >= 0 &&
            slot < container.GetSize() &&
            container.GetItem(slot) is not null)
        {
            return true;
        }

        if (player.Gamemode != Gamemode.Creative)
        {
            return false;
        }

        Container? output = player.GetTrait<PlayerCraftingOutputTrait>()?.Container;
        if (output is null || output.GetItem(0) is null)
        {
            return false;
        }

        container = output;
        slot = 0;
        return true;
    }

    private static ItemStack? ResolveCreativeCraftItem(uint creativeItemNetworkId)
    {
        if (CuratedItemCatalog.TryGetCreativeMenuItem(checked((int)creativeItemNetworkId), out CuratedItem curated))
        {
            ItemType? type = ItemType.GetByNetwork(curated.NetworkId);
            if (type is not null)
            {
                return new ItemStack(type, 1);
            }
        }

        ItemStack? item = ItemType.GetCreativeItem(creativeItemNetworkId);
        if (item is not null)
        {
            return item;
        }

        return ItemType.GetCreativePickFromSlotByte(checked((byte)creativeItemNetworkId));
    }

    private static string DescribeAction(IStackRequestAction action)
    {
        return action switch
        {
            TransferStackRequestAction transfer =>
                $"transfer count={transfer.Count} src={DescribeSlot(transfer.Source)} dst={DescribeSlot(transfer.Destination)}",
            SwapStackRequestAction swap =>
                $"swap src={DescribeSlot(swap.Source)} dst={DescribeSlot(swap.Destination)}",
            DropStackRequestAction drop =>
                $"drop count={drop.Count} src={DescribeSlot(drop.Source)}",
            DestroyStackRequestAction destroy =>
                $"destroy count={destroy.Count} src={DescribeSlot(destroy.Source)}",
            CraftCreativeStackRequestAction craftCreative =>
                $"craft_creative creative={craftCreative.CreativeItemNetworkId} count={craftCreative.NumberOfCrafts}",
            _ => action.GetType().Name
        };
    }

    private static string DescribeSlot(StackRequestSlotInfo slot)
    {
        string dynamicId = slot.Container.DynamicContainerId.HasValue
            ? slot.Container.DynamicContainerId.Value.ToString()
            : "none";

        return $"container={slot.Container.ContainerId} dynamic={dynamicId} slot={slot.Slot} stack={slot.StackNetworkId}";
    }
}
