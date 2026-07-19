namespace Orion.PluginContracts.Registry;

public sealed record ItemRegistration(
    string Identifier,
    bool Creative = true,
    int? CreativeCategory = null);
