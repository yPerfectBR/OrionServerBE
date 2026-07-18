namespace Orion.PluginContracts.Diagnostics;

public interface IPluginDiagnostics
{
    IReadOnlyList<PluginConflict> Conflicts { get; }

    IReadOnlyList<IPluginManifest> LoadedManifests { get; }
}
