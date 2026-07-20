using Orion.Containers;
using Orion.Item;

namespace Orion.Gameplay;

/// <summary>
/// Resolved player inventory handle for core and plugins (implemented by VanillaInventory).
/// </summary>
public interface IPlayerInventoryAccess
{
    IContainer Container { get; }

    int SelectedSlot { get; }

    void SetHeldSlot(int slot);

    ItemStack? GetHeldItem();

    void Clear();

    void SyncToPlayer(global::Orion.Player.Player player);

    void SyncHeldItemToClient(global::Orion.Player.Player player);
}
