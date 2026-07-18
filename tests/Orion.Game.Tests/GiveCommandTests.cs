using Orion.Commands;
using Orion.Commands.List.Operator;
using Orion.Item;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Registry;
using ProtocolCommand = Orion.Protocol.Types.Command;
using ProtocolCommandParameter = Orion.Protocol.Types.CommandParameter;

namespace Orion.Game.Tests;

public sealed class GiveCommandTests
{
    [Fact]
    public void ItemEnum_Options_OnlyIncludeRegisteredGiveableItems()
    {
        ItemRegistry.EnsureLoaded();

        ItemEnum itemEnum = new();
        IReadOnlyCollection<string> giveable = ItemRegistry.GetGiveableIdentifiers();

        Assert.Equal(giveable.Count, itemEnum.Options.Length);
        Assert.DoesNotContain("air", itemEnum.Options);
        Assert.Contains("grass_block", itemEnum.Options);
        Assert.Contains("dirt", itemEnum.Options);
        Assert.DoesNotContain("diamond", itemEnum.Options);
    }

    [Fact]
    public void GiveCommand_Overload_UsesItemNameParameter()
    {
        GiveCommand command = new();
        CommandParameter? itemName = command.Overload.Parameters.FirstOrDefault(parameter => parameter.Name == "itemName");
        Assert.NotNull(itemName);
        Assert.Equal(typeof(ItemEnum), itemName!.Enum);
        Assert.DoesNotContain(command.Overload.Parameters, parameter => parameter.Name == "item");
    }

    [Fact]
    public void GiveCommand_AvailableCommands_UsesSoftItemEnumWithImplementedItemsOnly()
    {
        CommandRegistry registry = new();
        registry.Register(new GiveCommand());

        AvailableCommandsPacket packet = registry.BuildAvailableCommandsPacket(new global::Orion.Server());

        ProtocolCommand? give = packet.Commands.FirstOrDefault(command => command.Name == "give");
        Assert.NotNull(give);

        ProtocolCommandParameter? itemName = give!.Overloads[0].Parameters.FirstOrDefault(parameter => parameter.Name == "itemName");
        Assert.NotNull(itemName);
        Assert.True((itemName!.Type & (uint)CommandParameterTypeFlag.SoftEnum) != 0);

        Assert.Single(packet.DynamicEnums);
        Assert.Equal("Item", packet.DynamicEnums[0].Type);
        Assert.Equal(5, packet.DynamicEnums[0].Values.Count);
        Assert.Contains("grass_block", packet.DynamicEnums[0].Values);
        Assert.Contains("dirt", packet.DynamicEnums[0].Values);
        Assert.DoesNotContain("cobblestone", packet.DynamicEnums[0].Values);
        Assert.DoesNotContain("diamond", packet.DynamicEnums[0].Values);
        Assert.DoesNotContain(packet.Enums, commandEnum => commandEnum.Type == "Item");
    }

    [Fact]
    public void ItemRegistry_CreativeItems_IncludeOnlyCreativeFlaggedItems()
    {
        ItemRegistry.EnsureLoaded();

        // CreativeItemNetworkId is 1-based (protocol): grass, dirt, bedrock (Nature only by default).
        Assert.NotNull(ItemType.GetCreativeItem(1));
        Assert.NotNull(ItemType.GetCreativeItem(2));
        Assert.NotNull(ItemType.GetCreativeItem(3));
        Assert.Null(ItemType.GetCreativeItem(0));
        Assert.Null(ItemType.GetCreativeItem(4));
        Assert.Null(ItemType.GetCreativeItem(999));

        Assert.Equal(
            ["minecraft:grass_block", "minecraft:dirt", "minecraft:bedrock"],
            CuratedItemCatalog.GetCreativeMenuItems().Select(i => i.Identifier).ToArray());
    }
}
