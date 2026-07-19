namespace Orion.PluginContracts.Diagnostics;

public sealed record PluginConflict(
    string Kind,
    string Key,
    string WinnerPluginId,
    string LoserPluginId,
    string Message);
