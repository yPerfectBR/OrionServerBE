namespace Orion.Item;

using Orion.Api.Traits;
using Orion.Protocol.Types;
using Orion.Protocol.Nbt;
using Orion.Item.Traits;
using ApiBlockPos = Orion.Api.Math.BlockPos;
using ApiItemBreakBlockDetails = Orion.Api.Traits.ItemBreakBlockDetails;
using ApiItemPlaceDetails = Orion.Api.Traits.ItemPlaceDetails;
using ApiItemUseAttackDetails = Orion.Api.Traits.ItemUseAttackDetails;
using ApiItemUseOnAirDetails = Orion.Api.Traits.ItemUseOnAirDetails;
using ApiItemUseOnBlockDetails = Orion.Api.Traits.ItemUseOnBlockDetails;
using ApiItemUseOnEntityDetails = Orion.Api.Traits.ItemUseOnEntityDetails;

public sealed class ItemStack : Orion.Api.Items.IItemStack {
    private static int _nextNetworkStackId;
    private readonly List<ItemTraitBase> _traits = [];

    public ItemType Type { get; }
    public string Identifier => Type.Identifier;
    public ushort StackSize { get; private set; }
    public uint Metadata { get; private set; }
    public int NetworkStackId { get; private set; } = ++_nextNetworkStackId;
    public ItemInstanceUserData? ExtraData { get; private set; }

    Orion.Api.Items.IItemType Orion.Api.Items.IItemStack.Type => Type;
    int Orion.Api.Items.IItemStack.Count => StackSize;
    uint Orion.Api.Items.IItemStack.Metadata => Metadata;
    int Orion.Api.Items.IItemStack.NetworkStackId => NetworkStackId;

    void Orion.Api.Items.IItemStack.SetCount(int count) => SetStackSize((ushort)Math.Clamp(count, 0, ushort.MaxValue));

    void Orion.Api.Items.IItemStack.Increment(int amount) => IncrementStack((ushort)Math.Clamp(amount, 0, ushort.MaxValue));

    void Orion.Api.Items.IItemStack.Decrement(int amount) => DecrementStack((ushort)Math.Clamp(amount, 0, ushort.MaxValue));

    bool Orion.Api.Items.IItemStack.CanStackWith(Orion.Api.Items.IItemStack other) =>
        other is ItemStack stack && CanStackWith(stack);

    Orion.Api.Items.IItemStack Orion.Api.Items.IItemStack.Clone(int? count) =>
        Clone(count is null ? null : (ushort)Math.Clamp(count.Value, 0, ushort.MaxValue));

    void Orion.Api.Items.IItemStack.NotifyBrokeBlock(
        Orion.Api.IPlayer player,
        ApiBlockPos blockPosition,
        int blockFace,
        int hotBarSlot)
    {
        OnBreakBlock(new ApiItemBreakBlockDetails(
            player,
            hotBarSlot,
            blockPosition,
            blockFace));
    }


    public ItemStack(ItemType type, ushort stackSize = 1, uint metadata = 0, ItemInstanceUserData? extraData = null)
    {
        Type = type;
        StackSize = (ushort)Math.Min(stackSize, type.MaxStackSize);
        Metadata = metadata;
        ExtraData = extraData;

        foreach (Type traitType in Type.Traits.Values)
        {
            if (Activator.CreateInstance(traitType, this) is ItemTraitBase trait)
            {
                AddTrait(trait);
            }
        }
    }
    
    public ItemStack(string identifier, ushort stackSize = 1, uint metadata = 0, ItemInstanceUserData? extraData = null)
        : this(ItemType.Get(identifier) ?? throw new InvalidOperationException($"Unknown item type '{identifier}'."), stackSize, metadata, extraData)
    {
    }

    public void SetStackSize(ushort value)
    {
        StackSize = (ushort)Math.Min(value, Type.MaxStackSize);
    }

    public void IncrementStack(ushort value = 1)
    {
        SetStackSize((ushort)(StackSize + value));
    }

    public void DecrementStack(ushort value = 1)
    {
        StackSize = value >= StackSize ? (ushort)0 : (ushort)(StackSize - value);
    }

    public void SetMetadata(uint value)
    {
        Metadata = value;
    }

    public void SetExtraData(ItemInstanceUserData? extraData)
    {
        ExtraData = extraData;
    }

    /// <summary>
    /// Adopts the stack network id predicted by the client for cursor placements.
    /// Required when server-authoritative inventory is enabled.
    /// </summary>
    internal void SetNetworkStackId(int networkStackId)
    {
        NetworkStackId = networkStackId;
    }

    public bool Equals(ItemStack other)
    {
        return Type.Identifier == other.Type.Identifier
               && StackSize == other.StackSize
               && Metadata == other.Metadata
               && Equals(ExtraData, other.ExtraData);
    }

    public bool CanStackWith(ItemStack other)
    {
        return Type.Identifier == other.Type.Identifier
               && Metadata == other.Metadata
               && HasSameTraits(other);
    }

    public LegacyItem ToNetworkStack()
    {
        LegacyItem descriptor = ItemType.ToNetworkStack(Type, StackSize, Metadata);
        descriptor.ItemStackId = NetworkStackId;
        descriptor.ExtraData = ExtraData;
        return descriptor;
    }

    public LegacyItem ToLegacyInventoryItem()
    {
        if (StackSize == 0 || Type.NetworkId == 0)
        {
            return new LegacyItem();
        }

        int networkBlockId = ItemBlockRuntimeIds.Resolve(Type);
        CompoundTag? nbt = GetSerializedNbt();
        bool hasExtraData = (ExtraData?.CanPlaceOn.Count ?? 0) > 0
            || (ExtraData?.CanDestroy.Count ?? 0) > 0
            || ExtraData?.Ticking is not null
            || (nbt is not null && nbt.Values.Count > 0);

        return new LegacyItem
        {
            NetworkId = Type.NetworkId,
            StackSize = StackSize,
            Metadata = unchecked((int)Metadata),
            ItemStackId = NetworkStackId,
            NetworkBlockId = networkBlockId,
            ExtraData = hasExtraData
                ? new ItemInstanceUserData
                {
                    Nbt = nbt,
                    CanPlaceOn = ExtraData?.CanPlaceOn ?? [],
                    CanDestroy = ExtraData?.CanDestroy ?? [],
                    Ticking = ExtraData?.Ticking
                }
                : null
        };
    }

    public NetworkItemStackDescriptor ToNetworkItemStackDescriptor()
    {
        if (StackSize == 0 || Type.NetworkId == 0)
        {
            return new NetworkItemStackDescriptor();
        }

        int runtimeId = ItemBlockRuntimeIds.Resolve(Type);

        return new NetworkItemStackDescriptor
        {
            NetworkId = Type.NetworkId,
            Count = StackSize,
            Metadata = Metadata,
            StackNetworkId = NetworkStackId,
            BlockRuntimeId = runtimeId,
            Nbt = GetSerializedNbt(),
            CanPlaceOn = ExtraData?.CanPlaceOn ?? [],
            CanDestroy = ExtraData?.CanDestroy ?? [],
            BlockingTick = ExtraData?.Ticking ?? 0
        };
    }

    public static ItemStack FromNetworkStack(LegacyItem descriptor)
    {
        ItemType type = ItemType.GetByNetwork(descriptor.NetworkId)
                        ?? throw new InvalidOperationException($"Unknown item network id '{descriptor.NetworkId}'.");

        ItemStack stack = new(type, descriptor.StackSize, unchecked((uint)descriptor.Metadata), descriptor.ExtraData);
        if (descriptor.ItemStackId is int stackId && stackId != 0)
        {
            stack.SetNetworkStackId(stackId);
        }

        return stack;
    }

    public static ItemStack FromNetworkStack(NetworkItemStackDescriptor descriptor)
    {
        ItemType type = ItemType.GetByNetwork(descriptor.NetworkId)
                        ?? throw new InvalidOperationException($"Unknown item network id '{descriptor.NetworkId}'.");

        bool hasExtra = descriptor.Nbt is not null
            || descriptor.CanPlaceOn.Count > 0
            || descriptor.CanDestroy.Count > 0
            || descriptor.BlockingTick != 0;

        ItemInstanceUserData? extraData = hasExtra
            ? new ItemInstanceUserData
            {
                Nbt = descriptor.Nbt,
                CanPlaceOn = descriptor.CanPlaceOn,
                CanDestroy = descriptor.CanDestroy,
                Ticking = descriptor.BlockingTick
            }
            : null;

        ItemStack stack = new(type, descriptor.Count, descriptor.Metadata, extraData);
        if (descriptor.StackNetworkId != 0)
        {
            stack.SetNetworkStackId(descriptor.StackNetworkId);
        }

        return stack;
    }

    public static ItemStack Empty()
    {
        return new ItemStack(ItemType.Air, 0, 0);
    }

    public CompoundTag Serialize()
    {
        CompoundTag tag = new();
        tag.Set("id", new StringTag { Value = Identifier });
        tag.Set("count", new IntTag { Value = StackSize });
        tag.Set("meta", new IntTag { Value = unchecked((int)Metadata) });

        CompoundTag? nbt = GetSerializedNbt();
        if (nbt is not null && nbt.Values.Count > 0)
        {
            tag.Set("nbt", nbt);
        }

        return tag;
    }

    public static ItemStack? Deserialize(CompoundTag tag)
    {
        StringTag? idTag = tag.Get<StringTag>("id");
        if (idTag is null || string.IsNullOrWhiteSpace(idTag.Value))
        {
            return null;
        }

        ItemType? type = ItemType.Get(idTag.Value);
        if (type is null)
        {
            return null;
        }

        ushort stackSize = (ushort)Math.Max(0, tag.Get<IntTag>("count")?.Value ?? 0);
        uint metadata = unchecked((uint)(tag.Get<IntTag>("meta")?.Value ?? 0));
        CompoundTag? nbt = tag.Get<CompoundTag>("nbt");
        ItemInstanceUserData? extraData = null;
        if (nbt is not null)
        {
            extraData = new ItemInstanceUserData
            {
                Nbt = nbt,
                CanPlaceOn = [],
                CanDestroy = [],
                Ticking = null
            };
        }

        ItemStack stack = new(type, stackSize, metadata, extraData);
        if (nbt is not null)
        {
            stack.ReadTraits(nbt);
        }
        return stack;
    }

    public T AddTrait<T>(T trait) where T : ItemTraitBase
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

    public CompoundTag? GetSerializedNbt()
    {
        CompoundTag nbt = ExtraData?.Nbt ?? new CompoundTag();
        WriteTraits(nbt);
        return nbt.Values.Count > 0 ? nbt : null;
    }

    public ItemStack Clone(ushort? stackSize = null)
    {
        CompoundTag serialized = Serialize();
        if (stackSize.HasValue)
        {
            serialized.Set("count", new IntTag { Value = stackSize.Value });
        }

        return Deserialize(serialized) ?? throw new InvalidOperationException("Failed to clone item stack.");
    }

    public void OnUseOnAir(ApiItemUseOnAirDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseOnAir(details);
        }
    }

    public void OnUseOnBlock(ApiItemUseOnBlockDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseOnBlock(details);
        }
    }

    public void OnPlace(ApiItemPlaceDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnPlace(details);
        }
    }

    public void OnUseOnEntity(ApiItemUseOnEntityDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseOnEntity(details);
        }
    }

    public void OnUseAttack(ApiItemUseAttackDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnUseAttack(details);
        }
    }

    public void OnBreakBlock(ApiItemBreakBlockDetails details)
    {
        for (int i = 0; i < _traits.Count; i++)
        {
            _traits[i].OnBreakBlock(details);
        }
    }


    public bool HasTrait<T>() where T : ItemTraitBase
    {
        return GetTrait<T>() is not null;
    }

    public T? GetTrait<T>() where T : ItemTraitBase
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

    private void WriteTraits(CompoundTag nbt)
    {
        if (_traits.Count == 0) return;

        ListTag traitsTag = new() { Name = "traits" };
        foreach (var trait in _traits)
        {
            if (trait is not ItemTrait hostTrait)
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

    private void ReadTraits(CompoundTag nbt)
    {
        ListTag? traitsTag = nbt.Get<ListTag>("traits");
        if (traitsTag is null) return;

        foreach (BaseTag tag in traitsTag.Values)
        {
            if (tag is not CompoundTag traitEntry) continue;

            string? identifier = traitEntry.Get<StringTag>("id")?.Value;
            CompoundTag? traitData = traitEntry.Get<CompoundTag>("data");

            if (identifier == null || traitData == null) continue;

            ItemTraitBase? trait = GetTrait(identifier);
            if (trait == null)
            {
                if (ItemTraitRegistry.RegisteredTraits.TryGetValue(identifier, out Type? traitType))
                {
                    if (Activator.CreateInstance(traitType, this) is ItemTraitBase newTrait)
                    {
                        AddTrait(newTrait);
                        trait = newTrait;
                    }
                }
            }

            if (trait is ItemTrait hostTrait)
            {
                hostTrait.OnRead(traitData);
            }
        }
    }

    // AddTrait methods are now defined above

    public ItemTraitBase? GetTrait(string identifier)
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

    private bool HasSameTraits(ItemStack other)
    {
        if (_traits.Count != other._traits.Count)
        {
            return false;
        }

        HashSet<string> thisTraits = new(_traits.Count, StringComparer.Ordinal);
        for (int i = 0; i < _traits.Count; i++)
        {
            thisTraits.Add(_traits[i].Identifier);
        }

        for (int i = 0; i < other._traits.Count; i++)
        {
            if (!thisTraits.Contains(other._traits[i].Identifier))
            {
                return false;
            }
        }

        return true;
    }
}






