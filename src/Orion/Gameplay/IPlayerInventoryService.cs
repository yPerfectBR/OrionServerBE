using Orion.Containers;
using Orion.Item;
using Orion.Protocol.Types;

namespace Orion.Gameplay;

/// <summary>
/// Opt-in player inventory API. Implemented by the VanillaInventory plugin.
/// </summary>
public interface IPlayerInventoryService
{
    bool TryOpenInventory(global::Orion.Player.Player player);

    bool TryCloseInventory(global::Orion.Player.Player player, int windowId);

    bool TryGetAccess(global::Orion.Player.Player player, out IPlayerInventoryAccess? access);

    ItemStack? GetHeldItem(global::Orion.Player.Player player);

    bool TrySetHeldSlot(global::Orion.Player.Player player, int slot);

    bool TryGive(global::Orion.Player.Player player, ItemStack stack, out int leftover);

    bool TryClear(global::Orion.Player.Player player);

    bool TryCollect(global::Orion.Player.Player player, ItemStack stack, out ushort moved);

    bool TrySyncToClient(global::Orion.Player.Player player);

    Container? ResolveContainer(global::Orion.Player.Player player, FullContainerName name);

    /// <summary>Process a client ItemStackRequest; returns false if inventory plugin is inactive.</summary>
    bool TryProcessItemStackRequest(
        global::Orion.Player.Player player,
        ItemStackRequest request,
        out ItemStackResponse response);
}
