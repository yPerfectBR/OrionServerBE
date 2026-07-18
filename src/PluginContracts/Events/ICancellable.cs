namespace Orion.PluginContracts.Events;

public interface ICancellable
{
    bool Cancelled { get; }

    void Cancel();
}
