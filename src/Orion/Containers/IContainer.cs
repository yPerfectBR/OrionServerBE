using Orion.Item;
using Orion.Protocol.Types;

namespace Orion.Containers;

/// <summary>
/// Core-facing container surface. Concrete storage/UI lives in the VanillaContainers plugin.
/// </summary>
public interface IContainer
{
    ContainerType Type { get; }

    int? Identifier { get; set; }

    int GetSize();

    ItemStack? GetItem(int slot);

    void SetItem(int slot, ItemStack item);

    bool AddItem(ItemStack item);

    void ClearSlot(int slot);

    void Clear();

    void Update();

    void UpdateSlot(int slot);

    int Show(global::Orion.Player.Player player);

    void Close(global::Orion.Player.Player player);

    bool RemoveViewer(global::Orion.Player.Player player, bool sendClose);

    IReadOnlyCollection<KeyValuePair<global::Orion.Player.Player, int>> GetAllOccupants();
}
