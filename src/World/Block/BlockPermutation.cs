using Orion.Protocol.Nbt;
using Orion.Protocol.Registry;

namespace Orion.World.Block;

/// <summary>
/// Minimal block permutation wrapper over curated Bedrock runtime IDs.
/// </summary>
public readonly struct BlockPermutation : IEquatable<BlockPermutation>
{
    private static readonly Dictionary<int, BlockPermutation> ByNetworkId = new();
    private static readonly Dictionary<string, BlockPermutation> ByIdentifier = new(StringComparer.Ordinal);

    public int NetworkId { get; init; }
    public string Identifier { get; init; }

    static BlockPermutation()
    {
        Register(BedrockBlockStates.Air, "minecraft:air");
        Register(BedrockBlockStates.Bedrock, "minecraft:bedrock");
        Register(BedrockBlockStates.Dirt, "minecraft:dirt");
        Register(BedrockBlockStates.GrassBlock, "minecraft:grass_block");
        Register(BedrockBlockStates.Barrier, "minecraft:barrier");
        Register(BedrockBlockStates.StructureVoid, "minecraft:structure_void");
    }

    public bool IsAir => NetworkId == BedrockBlockStates.Air;

    public static BlockPermutation Air => Resolve(BedrockBlockStates.Air);

    public static BlockPermutation Resolve(int networkId) =>
        ByNetworkId.TryGetValue(networkId, out BlockPermutation known)
            ? known
            : new BlockPermutation { NetworkId = networkId, Identifier = "minecraft:unknown" };

    public static BlockPermutation Resolve(string identifier) =>
        ByIdentifier.TryGetValue(identifier, out BlockPermutation known)
            ? known
            : Air;

    public static CompoundTag ToCompound(BlockPermutation permutation)
    {
        CompoundTag root = new();
        root.Set("name", new StringTag { Value = permutation.Identifier });
        root.Set("version", new IntTag { Value = 1 });
        root.Set("states", new CompoundTag());
        return root;
    }

    public static BlockPermutation FromCompound(CompoundTag nbt)
    {
        StringTag? name = nbt.Get<StringTag>("name");
        if (name is null)
        {
            throw new InvalidOperationException("Block permutation is missing the 'name' tag.");
        }

        return Resolve(name.Value);
    }

    public bool Equals(BlockPermutation other) => NetworkId == other.NetworkId;

    public override bool Equals(object? obj) => obj is BlockPermutation other && Equals(other);

    public override int GetHashCode() => NetworkId;

    public static bool operator ==(BlockPermutation left, BlockPermutation right) => left.Equals(right);

    public static bool operator !=(BlockPermutation left, BlockPermutation right) => !left.Equals(right);

    private static void Register(int networkId, string identifier)
    {
        BlockPermutation permutation = new() { NetworkId = networkId, Identifier = identifier };
        ByNetworkId[networkId] = permutation;
        ByIdentifier[identifier] = permutation;
    }
}
