namespace Orion.Commands;

using Orion.Item;


public class ItemEnum : CommandEnum
{
    const string VanillaPrefix = "minecraft:";

    public string Raw = string.Empty;

    public ItemType Type = ItemType.Air;

    public ItemEnum() : base("Item")
    {
        ItemRegistry.EnsureLoaded();
        Options = [.. ItemRegistry.GetGiveableIdentifiers().Select(TrimPrefix).Order(StringComparer.Ordinal)];
    }

    public ItemEnum(string raw, ItemType type) : base("Item")
    {
        Raw = raw;
        Type = type;
    }

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return false;
        }

        Raw = tokens[tokenIndex];
        string identifier = Raw.IndexOf(':') == -1 ? VanillaPrefix + Raw : Raw;
        if (!ItemRegistry.IsGiveable(identifier))
        {
            throw new InvalidOperationException($"Invalid item '{Raw}' for command parameter '{parameter.Name}'.");
        }

        Type = ItemType.Get(identifier) ?? throw new InvalidOperationException($"Invalid item '{Raw}' for command parameter '{parameter.Name}'.");
        tokenIndex++;
        return true;
    }

    static string TrimPrefix(string identifier)
    {
        if (!identifier.StartsWith(VanillaPrefix, StringComparison.Ordinal))
        {
            return identifier;
        }

        return identifier[VanillaPrefix.Length..];
    }
}







