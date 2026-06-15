using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Orion.Item.Traits;
using Orion.Protocol.Io;
using Orion.Protocol.Nbt;
using Orion.Config;
using Log = Orion.Logger.Logger;

namespace Orion.Item;

public static class ItemRegistry
{
    private static readonly object LoadLock = new();
    private static bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    private static Dictionary<uint, ItemStack>? _creativeItems;
    private static HashSet<string> _giveableIdentifiers = new(StringComparer.Ordinal);

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

            Block.BlockRegistry.EnsureLoaded();
            string path = Path.Combine(ResolveDataRoot(), "items.json");
            List<ItemRegistryDto>? items = JsonSerializer.Deserialize<List<ItemRegistryDto>>(File.ReadAllBytes(path), JsonOptions) ?? [];
            _giveableIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemRegistryDto dto in items)
            {
                _giveableIdentifiers.Add(dto.Identifier);
                CompoundTag properties = ReadPropertiesNbt(dto.PropertiesBase64);
                _ = new ItemType(
                    dto.Identifier,
                    dto.NetworkId,
                    maxStackSize: dto.MaxStackSize > 0 ? dto.MaxStackSize : 64,
                    tags: null,
                    isComponentBased: dto.ComponentBased,
                    version: dto.ItemVersion,
                    properties: properties);
            }

            _ = ItemType.GetOrAir("minecraft:air");
            LoadCreativeItems(items);
            ItemTraitRegistry.RegisterFromAssembly(Assembly.GetExecutingAssembly());
            _loaded = true;
        }
    }

    public static ItemStack? TryGetCreativeItem(uint creativeItemNetworkId)
    {
        EnsureLoaded();
        return _creativeItems is not null && _creativeItems.TryGetValue(creativeItemNetworkId, out ItemStack? item)
            ? item.Clone()
            : null;
    }

    public static ItemStack? TryGetCreativePickFromSlotByte(byte slotByte, out string resolution)
    {
        EnsureLoaded();
        resolution = "none";

        ItemType? bySignedNetwork = ItemType.GetByNetwork((sbyte)slotByte);
        if (bySignedNetwork is not null && bySignedNetwork != ItemType.Air)
        {
            resolution = $"networkId(sbyte)={(sbyte)slotByte} -> {bySignedNetwork.Identifier}";
            return new ItemStack(bySignedNetwork, 1);
        }

        ItemType? byNetwork = ItemType.GetByNetwork(slotByte);
        if (byNetwork is not null && byNetwork != ItemType.Air)
        {
            resolution = $"networkId={slotByte} -> {byNetwork.Identifier}";
            return new ItemStack(byNetwork, 1);
        }

        if (_creativeItems is not null)
        {
            foreach (ItemStack item in _creativeItems.Values)
            {
                int networkId = item.Type.NetworkId;
                if (networkId == slotByte
                    || networkId == (sbyte)slotByte
                    || unchecked((byte)networkId) == slotByte)
                {
                    resolution = $"creativeCatalog networkId={networkId} slotByte={slotByte} -> {item.Type.Identifier}";
                    return item.Clone();
                }
            }
        }

        ItemStack? fromIndex = TryGetCreativeItem(slotByte);
        if (fromIndex is not null)
        {
            resolution = $"creativeIndex={slotByte} -> {fromIndex.Type.Identifier}";
            return fromIndex;
        }

        return null;
    }

    public static ItemStack? TryGetCreativePickFromSlotByte(byte slotByte) =>
        TryGetCreativePickFromSlotByte(slotByte, out _);

    public static IReadOnlyCollection<string> GetGiveableIdentifiers()
    {
        EnsureLoaded();
        return _giveableIdentifiers;
    }

    public static bool IsGiveable(string identifier)
    {
        EnsureLoaded();
        return _giveableIdentifiers.Contains(identifier);
    }

    static void LoadCreativeItems(List<ItemRegistryDto> items)
    {
        Dictionary<uint, ItemStack> creative = [];
        uint creativeIndex = 0;
        foreach (ItemRegistryDto dto in items)
        {
            if (!dto.Creative)
            {
                continue;
            }

            ItemType? type = ItemType.GetByNetwork(dto.NetworkId);
            if (type is not null)
            {
                creative[creativeIndex++] = new ItemStack(type, 1);
            }
        }

        _creativeItems = creative;
        Log.Info(
            LogCategory.Orion,
            "[CreativeInv] ItemRegistry loaded creativeCount={0}",
            creative.Count);
    }

    private static string ResolveDataRoot()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Protocol", "Data", "orion"),
            Path.Combine(AppContext.BaseDirectory, "Data", "orion"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Protocol", "Data", "orion"))
        ];
        foreach (string path in candidates)
        {
            if (File.Exists(Path.Combine(path, "items.json")))
            {
                return path;
            }
        }

        throw new FileNotFoundException("Could not locate Protocol/Data/orion/items.json");
    }

    private sealed class ItemRegistryDto
    {
        public int NetworkId { get; set; }
        public string Identifier { get; set; } = "";
        public bool ComponentBased { get; set; }
        public int ItemVersion { get; set; }
        public bool Creative { get; set; } = true;
        public int MaxStackSize { get; set; }
        public string PropertiesBase64 { get; set; } = "CgAAAA==";
    }

    static CompoundTag ReadPropertiesNbt(string propertiesBase64)
    {
        if (string.IsNullOrWhiteSpace(propertiesBase64))
        {
            return new CompoundTag();
        }

        byte[] payload = Convert.FromBase64String(propertiesBase64);
        if (payload.Length == 0)
        {
            return new CompoundTag();
        }

        int offset = 0;
        Basalt.Binary.BinaryReader reader = new(payload, ref offset);
        return NBT.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }
}
