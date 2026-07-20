namespace Orion.PluginContracts;

public enum PluginSoftLoadOrder
{
    After,
    Before
}

/// <summary>Hard or soft dependency edge with inclusive SemVer range.</summary>
public class PluginDependency
{
    public required string Id { get; init; }
    public required Version MinVersion { get; init; }
    public required Version MaxVersion { get; init; }

    public bool Contains(Version version) =>
        version >= MinVersion && version <= MaxVersion;
}

/// <summary>Optional dependency with explicit load direction when the target exists.</summary>
public sealed class PluginSoftDependency : PluginDependency
{
    public PluginSoftLoadOrder Load { get; init; } = PluginSoftLoadOrder.After;
}
