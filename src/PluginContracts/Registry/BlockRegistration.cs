namespace Orion.PluginContracts.Registry;

public sealed record BlockRegistration(
    string Identifier,
    int DefaultStateHash,
    bool Solid = true,
    bool Air = false);
