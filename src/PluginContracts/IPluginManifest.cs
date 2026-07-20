namespace Orion.PluginContracts;

public interface IPluginManifest
{
    string Id { get; }
    Version Version { get; }
    Version ApiVersion { get; }
    IReadOnlyList<PluginDependency> Depend { get; }
    IReadOnlyList<PluginSoftDependency> SoftDepend { get; }
    IReadOnlyList<string> Provides { get; }
    string Main { get; }
}
