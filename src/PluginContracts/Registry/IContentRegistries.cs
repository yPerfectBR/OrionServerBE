namespace Orion.PluginContracts.Registry;

public interface IContentRegistries
{
    IItemRegistry Items { get; }
    IBlockRegistry Blocks { get; }
    ICommandRegistry Commands { get; }
    ICreativeTabRegistry CreativeTabs { get; }
    IGeneratorRegistry Generators { get; }
}
