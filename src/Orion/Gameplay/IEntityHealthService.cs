using Orion.Protocol.Enums;

namespace Orion.Gameplay;

/// <summary>
/// Opt-in entity health API. Implemented by the VanillaAttributes plugin.
/// </summary>
public interface IEntityHealthService
{
    bool TryApplyDamage(
        global::Orion.Entity.Entity entity,
        float amount,
        global::Orion.Entity.Entity? damager = null,
        ActorDamageCause? cause = null);

    bool TryHeal(global::Orion.Entity.Entity entity, float amount);

    bool TryGet(global::Orion.Entity.Entity entity, out float current, out float maximum);

    bool TrySet(global::Orion.Entity.Entity entity, float current);
}
