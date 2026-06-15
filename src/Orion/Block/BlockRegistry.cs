using System.Reflection;
using System.Runtime.CompilerServices;
using Orion.Block.Traits;
using Orion.Block.Types;
using Orion.Protocol.Registry;

namespace Orion.Block;

public static class BlockRegistry
{
    private static readonly object LoadLock = new();
    private static bool _loaded;

    [ModuleInitializer]
    public static void Initialize() => EnsureLoaded();

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        lock (LoadLock)
        {
            if (_loaded)
            {
                return;
            }

            RegisterFromBedrockStates();
            BlockTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
            _loaded = true;
        }
    }

    static void RegisterFromBedrockStates()
    {
        RegisterBlock("minecraft:air", BedrockBlockStates.Air, air: true, solid: false);
        RegisterBlock("minecraft:structure_void", BedrockBlockStates.StructureVoid, solid: false);
        RegisterBlock("minecraft:bedrock", BedrockBlockStates.Bedrock);
        RegisterBlock("minecraft:dirt", BedrockBlockStates.Dirt);
        RegisterBlock("minecraft:grass_block", BedrockBlockStates.GrassBlock);
        RegisterBlock("minecraft:barrier", BedrockBlockStates.Barrier, solid: false);
    }

    static void RegisterBlock(string identifier, int hash, bool air = false, bool solid = true)
    {
        BlockType type = BlockType.Get(identifier) ?? new BlockType(identifier);
        type.Air = air;
        type.Solid = solid;
        BlockState state = [];
        BlockPermutation permutation = new(hash, state, type);
        BlockPermutation.Permutations[hash] = permutation;
        type.RegisterPermutation(permutation);
    }
}
