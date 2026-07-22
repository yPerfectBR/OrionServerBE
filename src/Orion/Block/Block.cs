namespace Orion.Block;

using Orion.Protocol.Nbt;
using Orion.Protocol.Enums;
using Orion.Protocol.Types;
using Orion.Entity;
using Orion.Entity.Traits.Types;
using Orion.Api.Traits;
using Orion.Block.Traits;
using Orion.Block.Traits.Types;
using Orion.Block.Types;
using Orion.Item;
using ApiBlockPlaceDetails = Orion.Api.Traits.BlockPlaceDetails;



public sealed class Block : Orion.Api.Blocks.IBlock
{
    private readonly List<BlockTraitBase> _traits = [];

    public BlockType Type { get; }
    public BlockPermutation Permutation { get; private set; }
    public string Identifier => Type.Identifier;

    Orion.Api.Blocks.IBlockType Orion.Api.Blocks.IBlock.Type => Type;
    Orion.Api.Blocks.IBlockPermutation Orion.Api.Blocks.IBlock.Permutation => Permutation;

    public void NotifyBroken(Orion.Api.IPlayer breaker, Orion.Api.Math.BlockPos blockPosition)
    {
        if (breaker is not Orion.Player.Player player)
        {
            throw new ArgumentException("Breaker must be a host player instance.", nameof(breaker));
        }

        OnBreak(new BlockBreakDetails(
            player,
            new BlockPos { X = blockPosition.X, Y = blockPosition.Y, Z = blockPosition.Z }));
    }

    public bool TryGetStateInt(string key, out int value)
    {
        if (Permutation.State.TryGetValue(key, out BlockStateValue state) && state.Kind == 0)
        {
            value = (int)state.AsNumber();
            return true;
        }

        value = 0;
        return false;
    }

    public bool TryGetStateString(string key, out string value)
    {
        if (Permutation.State.TryGetValue(key, out BlockStateValue state) && state.Kind == 1)
        {
            value = state.AsString();
            return true;
        }

        value = "";
        return false;
    }

    public void SetStateInt(string key, int value) => SetState(key, value);

    public void SetStateString(string key, string value) => SetState(key, value);

    void SetState(string key, BlockStateValue value)
    {
        BlockState state = [];
        foreach ((string existingKey, BlockStateValue existing) in Permutation.State)
        {
            state[existingKey] = existing;
        }

        state[key] = value;
        SetPermutation(Type.GetPermutation(state));
    }

    public Block(BlockType type, BlockPermutation permutation)
    {
        Type = type;
        Permutation = permutation;

        foreach (Type traitType in Type.Traits.Values)
        {
            if (Activator.CreateInstance(traitType, this) is BlockTraitBase trait)
            {
                AddTrait(trait);
            }
        }
    }

    public Block(string identifier)
        : this(BlockType.GetOrAir(identifier), BlockType.GetOrAir(identifier).GetPermutation())
    {
    }

    public Block(BlockPermutation permutation)
        : this(permutation.Type, permutation)
    {
    }

    public void SetPermutation(BlockPermutation permutation)
    {
        if (permutation.Type.Identifier != Type.Identifier)
        {
            throw new ArgumentException("Cannot set permutation for a different block type.", nameof(permutation));
        }
        Permutation = permutation;
    }

    public T AddTrait<T>(T trait) where T : BlockTraitBase
    {
        ArgumentNullException.ThrowIfNull(trait);
        if (GetTrait(trait.Identifier) is not null)
        {
            return trait;
        }

        _traits.Add(trait);
        trait.OnAdd();
        return trait;
    }

    public bool HasTrait<T>() where T : BlockTraitBase
    {
        return GetTrait<T>() is not null;
    }

    public T? GetTrait<T>() where T : BlockTraitBase
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    public void OnPlace(ApiBlockPlaceDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnPlace(details);
        }
    }

    public void OnBreak(BlockBreakDetails details)
    {
        if (details.Player.Gamemode != Gamemode.Creative && details.Player.Dimension is { } dimension)
        {
            List<ItemStack> drops = BlockDropHelper.GenerateLootFromBlock(this);
            for (int i = 0; i < drops.Count; i++)
            {
                ItemEntity drop = new(drops[i])
                {
                    Position = new Vec3f
                    {
                        X = details.BlockPosition.X + 0.5f,
                        Y = details.BlockPosition.Y + 0.5f,
                        Z = details.BlockPosition.Z + 0.5f
                    }
                };

                drop.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
            }
        }

        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is BlockTrait hostTrait)
            {
                hostTrait.OnBreak(details);
            }
        }
    }

    public void OnInteract(BlockInteractDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is BlockTrait hostTrait)
            {
                hostTrait.OnInteract(details);
            }
        }
    }

    public void OnTick(BlockTickDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is BlockTrait hostTrait)
            {
                hostTrait.OnTick(details);
            }
        }
    }

    public void OnRandomTick(BlockRandomTickDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is BlockTrait hostTrait)
            {
                hostTrait.OnRandomTick(details);
            }
        }
    }

    public void OnLandOn(BlockLandOnDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is BlockTrait hostTrait)
            {
                hostTrait.OnLandOn(details);
            }
        }
    }

    public void OnRender(Orion.Player.Player player, int x, int y, int z)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (_traits[i] is BlockTrait hostTrait)
            {
                hostTrait.OnRender(player, x, y, z);
            }
        }
    }

    public BlockTraitBase? GetTrait(string identifier)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            if (string.Equals(_traits[i].Identifier, identifier, StringComparison.Ordinal))
            {
                return _traits[i];
            }
        }

        return null;
    }

    public void WriteTraits(CompoundTag nbt)
    {
        if (_traits.Count == 0) return;

        ListTag traitsTag = new() { Name = "traits" };
        foreach (var trait in _traits)
        {
            if (trait is not BlockTrait hostTrait)
            {
                continue;
            }

            CompoundTag traitEntry = new();
            traitEntry.Set("id", new StringTag { Value = hostTrait.Identifier });

            CompoundTag traitData = new();
            hostTrait.OnWrite(traitData);
            traitEntry.Set("data", traitData);

            traitsTag.Values.Add(traitEntry);
        }

        if (traitsTag.Values.Count > 0)
        {
            nbt.Set("traits", traitsTag);
        }
    }

    public void ReadTraits(CompoundTag nbt)
    {
        ListTag? traitsTag = nbt.Get<ListTag>("traits");
        if (traitsTag is null)
        {
            for (int i = 0; i < _traits.Count; i++)
            {
                if (_traits[i] is BlockTrait hostTrait)
                {
                    hostTrait.OnRead(nbt);
                }
            }

            return;
        }

        foreach (BaseTag tag in traitsTag.Values)
        {
            if (tag is not CompoundTag traitEntry) continue;

            string? identifier = traitEntry.Get<StringTag>("id")?.Value;
            CompoundTag? traitData = traitEntry.Get<CompoundTag>("data");

            if (identifier == null || traitData == null) continue;

            BlockTraitBase? trait = GetTrait(identifier);
            if (trait == null)
            {
                if (BlockTraitRegistry.RegisteredTraits.TryGetValue(identifier, out Type? traitType))
                {
                    if (Activator.CreateInstance(traitType, this) is BlockTraitBase newTrait)
                    {
                        AddTrait(newTrait);
                        trait = newTrait;
                    }
                }
            }

            if (trait is BlockTrait hostTrait)
            {
                hostTrait.OnRead(traitData);
            }
        }
    }
}
