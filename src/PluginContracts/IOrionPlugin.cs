namespace Orion.PluginContracts;

/// <summary>
/// Opt-in C# plugin. Loaded exclusively by McMaster via the host PluginHost.
/// </summary>
public interface IOrionPlugin
{
    string Id { get; }

    Version Version { get; }

    /// <summary>After McMaster load, before Server exists. Pre-catalog registration only.</summary>
    void Load(IPluginLoadContext context);

    /// <summary>After ServerHost.Bootstrap.</summary>
    void OnEnable(IPluginContext context);

    /// <summary>World ready for content registration.</summary>
    void OnWorldInitialize(IWorldInitContext context);

    void OnDisable(IPluginContext context);
}
