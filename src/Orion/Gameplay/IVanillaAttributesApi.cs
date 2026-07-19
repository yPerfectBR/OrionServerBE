namespace Orion.Gameplay;

/// <summary>
/// Facade for vanilla vitals (health + hunger). Soft-depend on plugin id
/// <c>VanillaAttributes</c> / provides <c>orion:attributes</c>, then
/// <c>context.Services.TryGet&lt;IVanillaAttributesApi&gt;(...)</c>.
/// </summary>
public interface IVanillaAttributesApi
{
    IEntityHealthService Health { get; }
    IPlayerHungerService Hunger { get; }
}
