namespace Orion.Player.Traits;

/// <summary>
/// Opt-in trait hook for ticks executed by SessionWorker.
/// Use only for logic that is safe on the session-thread context.
/// </summary>
public interface ISessionTickableTrait
{
    void OnSessionTick();
}
