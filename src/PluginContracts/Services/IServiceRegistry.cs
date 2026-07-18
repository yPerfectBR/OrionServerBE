namespace Orion.PluginContracts.Services;

public interface IServiceRegistry
{
    void Register<TService>(TService instance, IOrionPlugin owner, ServicePriority priority = ServicePriority.Normal)
        where TService : class;

    void UnregisterAll(IOrionPlugin owner);

    bool TryGet<TService>(out TService? service) where TService : class;

    /// <summary>Highest priority registration wins for TryGet.</summary>
    TService GetRequired<TService>() where TService : class;
}
