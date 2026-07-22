namespace Orion.Item;

using Orion.Block;
using Orion.Item.Components;
using Orion.Item.Traits;
using Orion.Protocol.Nbt;
using Orion.Protocol.Types;


public sealed class ItemType : Orion.Api.Items.IItemType
{
    private static readonly Dictionary<string, ItemType> Registry = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, ItemType> NetworkRegistry = [];

    public string Identifier { get; }
    public int NetworkId { get; }
    public int MaxStackSize { get; }
    public bool IsComponentBased { get; }
    public int Version { get; }
    public IReadOnlyList<string> Tags { get; }
    public CompoundTag Properties { get; }
    public BlockType? BlockType { get; }
    public ItemTypeComponentCollection Components { get; }
    public IReadOnlyDictionary<string, Type> Traits => _traits;

    private readonly Dictionary<string, Type> _traits = new(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, ItemType> Types => Registry;
    public static ItemType Air => GetOrAir("minecraft:air");

    public ItemType(
        string identifier,
        int networkId,
        int maxStackSize,
        IEnumerable<string>? tags,
        bool isComponentBased,
        int version,
        CompoundTag? properties = null)
    {
        Identifier = identifier;
        NetworkId = networkId;
        MaxStackSize = maxStackSize;
        IsComponentBased = isComponentBased;
        Version = version;
        Tags = tags is null ? [] : [.. tags];
        Properties = properties ?? new CompoundTag();
        Components = new ItemTypeComponentCollection(this, Properties);
        BlockType = BlockType.Get(identifier);

        Registry[identifier] = this;
        NetworkRegistry[networkId] = this;
        ItemTraitRegistry.BindTraitsToType(this);
    }

    public static ItemType? Get(string identifier)
    {
        return Registry.TryGetValue(identifier, out ItemType? type) ? type : null;
    }

    internal static void ResetForTests()
    {
        Registry.Clear();
        NetworkRegistry.Clear();
    }

    public static ItemType GetOrAir(string identifier)
    {
        return Get(identifier) ?? Get("minecraft:air") ?? new ItemType("minecraft:air", 0, 64, [], true, 1);
    }

    public static ItemType? GetByNetwork(int networkId)
    {
        return NetworkRegistry.TryGetValue(networkId, out ItemType? type) ? type : null;
    }

    public static List<ItemType> GetAll()
    {
        return [.. Registry.Values];
    }

    public static ItemStack? GetCreativeItem(uint creativeItemNetworkId)
    {
        ItemRegistry.EnsureLoaded();
        return ItemRegistry.TryGetCreativeItem(creativeItemNetworkId);
    }

    public static ItemStack? GetCreativePickFromSlotByte(byte slotByte, out string resolution)
    {
        ItemRegistry.EnsureLoaded();
        return ItemRegistry.TryGetCreativePickFromSlotByte(slotByte, out resolution);
    }

    public static ItemStack? GetCreativePickFromSlotByte(byte slotByte)
    {
        ItemRegistry.EnsureLoaded();
        return ItemRegistry.TryGetCreativePickFromSlotByte(slotByte);
    }

    public void RegisterTrait(Type traitType, string identifier)
    {
        if (!typeof(Orion.Api.Traits.ItemTraitBase).IsAssignableFrom(traitType) || traitType.IsAbstract)
        {
            return;
        }

        _traits.TryAdd(identifier, traitType);
    }

    public bool TryGetComponentProperties(string component, out CompoundTag properties)
    {
        return Components.TryGetComponentProperties(component, out properties);
    }

    public bool TryGetFood(
        out int nutrition,
        out float saturationModifier,
        out bool canAlwaysEat,
        out string? usingConvertsTo)
    {
        ItemTypeFoodComponent? food = Components.GetComponent<ItemTypeFoodComponent>();
        if (food is null)
        {
            nutrition = 0;
            saturationModifier = 0f;
            canAlwaysEat = false;
            usingConvertsTo = null;
            return false;
        }

        nutrition = food.GetNutrition();
        saturationModifier = food.GetSaturationModifier();
        canAlwaysEat = food.CanAlwaysEat();
        string converts = food.GetUsingConvertsTo();
        usingConvertsTo = string.IsNullOrWhiteSpace(converts) ? null : converts;
        return true;
    }

    public bool TryGetUseDurationTicks(out ulong ticks)
    {
        ticks = 0UL;

        if (TryGetComponentProperties("minecraft:use_duration", out CompoundTag durationTag))
        {
            int value = durationTag.Get<IntTag>("value")?.Value
                        ?? durationTag.Get<IntTag>("use_duration")?.Value
                        ?? 0;
            if (value > 0)
            {
                ticks = (ulong)value;
                return true;
            }
        }

        CompoundTag? components = Properties.Get<CompoundTag>("components");
        CompoundTag? itemProperties = components?.Get<CompoundTag>("item_properties")
                                      ?? components?.Get<CompoundTag>("minecraft:item_properties");
        IntTag? useDuration = itemProperties?.Get<IntTag>("use_duration");
        if (useDuration is not null && useDuration.Value > 0)
        {
            ticks = (ulong)useDuration.Value;
            return true;
        }

        return false;
    }

    public bool TryGetDurability(out int maxDurability, out int damageChanceMin, out int damageChanceMax)
    {
        ItemTypeDurabilityComponent? durability = Components.GetComponent<ItemTypeDurabilityComponent>();
        if (durability is null)
        {
            maxDurability = 0;
            damageChanceMin = 0;
            damageChanceMax = 0;
            return false;
        }

        maxDurability = durability.GetMaxDurability();
        (damageChanceMin, damageChanceMax) = durability.GetDamageChance();
        return true;
    }

    public static void EnsureRegistryCapacity(int capacity)
    {
        Registry.EnsureCapacity(capacity);
        NetworkRegistry.EnsureCapacity(capacity);
    }

    public static LegacyItem ToNetworkStack(ItemType type, ushort stackSize = 1, uint metadata = 0)
    {
        int networkBlockId = ItemBlockRuntimeIds.Resolve(type);

        return new LegacyItem
        {
            NetworkId = type.NetworkId,
            StackSize = stackSize,
            Metadata = unchecked((int)metadata),
            ItemStackId = null,
            NetworkBlockId = networkBlockId,
            ExtraData = new ItemInstanceUserData
            {
                Nbt = null,
                CanPlaceOn = [],
                CanDestroy = [],
                Ticking = null
            }
        };
    }
}






