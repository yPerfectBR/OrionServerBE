namespace Orion.Gameplay;

/// <summary>
/// Facade for vanilla block crack / destroy.
/// Soft-depend on <c>VanillaMining</c> / provides <c>orion:mining</c>.
/// </summary>
public interface IVanillaMiningApi
{
    IPlayerBlockBreakHandler BlockBreak { get; }
}
