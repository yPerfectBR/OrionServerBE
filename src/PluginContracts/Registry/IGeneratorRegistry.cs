namespace Orion.PluginContracts.Registry;

public interface IGeneratorRegistry
{
    void Register(string name, Type generatorType);
}
