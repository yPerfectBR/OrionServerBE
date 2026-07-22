namespace Orion.Api;

/// <summary>Optional debug tip HUD for players (phase 27 plugin).</summary>
public interface IPlayerDebugHud
{
    void SetMode(PlayerDebugHudMode mode);
    PlayerDebugHudMode GetMode();
}

public enum PlayerDebugHudMode
{
    Off = 0,
    Simplified = 1,
    Full = 2
}
