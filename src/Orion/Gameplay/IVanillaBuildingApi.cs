namespace Orion.Gameplay;

/// <summary>
/// Facade for vanilla block placement / item-use-on-block.
/// Soft-depend on <c>VanillaBuilding</c> / provides <c>orion:building</c>.
/// </summary>
public interface IVanillaBuildingApi
{
    IPlayerBlockUseHandler BlockUse { get; }
}
