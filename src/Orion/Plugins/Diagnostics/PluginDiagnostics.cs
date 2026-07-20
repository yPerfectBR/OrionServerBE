using Orion.Config;
using Orion.PluginContracts;
using Orion.PluginContracts.Diagnostics;
using Log = Orion.Logger.Logger;

namespace Orion.Plugins.Diagnostics;

public sealed class PluginDiagnostics : IPluginDiagnostics
{
    readonly object _sync = new();
    readonly List<PluginConflict> _conflicts = [];
    Func<IReadOnlyList<IPluginManifest>> _manifestsProvider = static () => [];

    public ConflictMode ConflictMode { get; set; } = ConflictMode.Warn;

    public IReadOnlyList<PluginConflict> Conflicts
    {
        get
        {
            lock (_sync)
            {
                return _conflicts.ToArray();
            }
        }
    }

    public IReadOnlyList<IPluginManifest> LoadedManifests => _manifestsProvider();

    public void SetManifestsProvider(Func<IReadOnlyList<IPluginManifest>> provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _manifestsProvider = provider;
    }

    /// <summary>
    /// Records a conflict and logs a warning. When <paramref name="applyConflictMode"/> is true and
    /// <see cref="ConflictMode"/> is Fail, throws <see cref="InvalidOperationException"/>.
    /// Pass <c>false</c> for multi-register services (never reject).
    /// </summary>
    public void Report(PluginConflict conflict, bool applyConflictMode = true)
    {
        ArgumentNullException.ThrowIfNull(conflict);
        lock (_sync)
        {
            _conflicts.Add(conflict);
        }

        Log.Warn(
            LogCategory.Plugins,
            "Plugin conflict [{0}] {1}: {2} > {3} — {4}",
            conflict.Kind,
            conflict.Key,
            conflict.WinnerPluginId,
            conflict.LoserPluginId,
            conflict.Message);

        if (applyConflictMode)
        {
            ApplyConflictMode(conflict);
        }
    }

    public void ApplyConflictMode(PluginConflict conflict)
    {
        if (ConflictMode != ConflictMode.Fail)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(conflict);
        throw new InvalidOperationException(
            $"Plugin conflict [{conflict.Kind}] {conflict.Key}: {conflict.WinnerPluginId} > {conflict.LoserPluginId}. {conflict.Message}");
    }

    public void ResetForTests()
    {
        lock (_sync)
        {
            _conflicts.Clear();
            ConflictMode = ConflictMode.Warn;
        }
    }
}
