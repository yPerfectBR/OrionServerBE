namespace Orion.Gameplay;

/// <summary>
/// Opt-in player hunger API. Implemented by the VanillaAttributes plugin.
/// </summary>
public interface IPlayerHungerService
{
    bool TryEat(
        global::Orion.Player.Player player,
        int nutrition,
        float saturationModifier,
        bool canAlwaysEat);

    bool TryAddExhaustion(global::Orion.Player.Player player, float amount);

    bool TryGet(
        global::Orion.Player.Player player,
        out float hunger,
        out float saturation,
        out float exhaustion);

    bool TrySetHunger(
        global::Orion.Player.Player player,
        float hunger,
        float? saturation = null);
}
