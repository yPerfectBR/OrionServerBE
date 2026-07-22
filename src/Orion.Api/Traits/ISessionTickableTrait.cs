namespace Orion.Api.Traits;

/// <summary>Opt-in trait hook for ticks executed by the session worker.</summary>
public interface ISessionTickableTrait
{
    void OnSessionTick();
}
