using Orion.Item;

namespace Orion.Gameplay;

/// <summary>
/// Optional gameplay hook for held-item use (eat, drink, …). Registered by plugins via
/// <see cref="PluginContracts.Services.IServiceRegistry"/>.
/// </summary>
public interface IPlayerItemUseHandler
{
    /// <summary>Returns false when the held item cannot start a use action.</summary>
    bool TryBeginUse(global::Orion.Player.Player player, ItemStack heldItem, out ulong durationTicks);

    /// <summary>Applies effects when a pending use finishes. Return true if the action succeeded.</summary>
    bool TryCompleteUse(global::Orion.Player.Player player, ItemStack heldItem, int slot);
}
