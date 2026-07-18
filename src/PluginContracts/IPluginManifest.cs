namespace Orion.PluginContracts;

public interface IPluginManifest
{
    string Id { get; }
    Version Version { get; }
    Version ApiVersion { get; }
    IReadOnlyList<string> Depend { get; }
    IReadOnlyList<string> SoftDepend { get; }
    IReadOnlyList<string> LoadBefore { get; }
    IReadOnlyList<string> Provides { get; }
    string Main { get; }
}
