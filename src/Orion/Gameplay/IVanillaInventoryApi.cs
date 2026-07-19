namespace Orion.Gameplay;

/// <summary>
/// Facade for vanilla player inventory. Soft-depend on <c>VanillaInventory</c> /
/// provides <c>orion:inventory</c>.
/// </summary>
public interface IVanillaInventoryApi
{
    IPlayerInventoryService Inventory { get; }
}
