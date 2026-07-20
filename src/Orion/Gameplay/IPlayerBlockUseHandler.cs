using Orion.Protocol.Types;

namespace Orion.Gameplay;

/// <summary>
/// Opt-in block/air item use (place, interact, use-on-block). Implemented by VanillaBuilding.
/// </summary>
public interface IPlayerBlockUseHandler
{
    bool TryUseOnBlock(global::Orion.Player.Player player, UseItemInventoryTransactionData data);

    bool TryUseOnAir(global::Orion.Player.Player player, UseItemInventoryTransactionData data);
}
