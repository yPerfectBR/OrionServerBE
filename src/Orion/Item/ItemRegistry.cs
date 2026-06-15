using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Orion.Item.Traits;
using Orion.Protocol.Nbt;

namespace Orion.Item;

public static class ItemRegistry
{
    private static readonly object LoadLock = new();
    private static bool _loaded;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static Dictionary<uint, ItemStack>? _creativeItems;

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
            List<ItemRegistryDto>? items = JsonSerializer.Deserialize<List<ItemRegistryDto>>(File.ReadAllBytes(path), JsonOptions);
            if (items is not null)
            {
                foreach (ItemRegistryDto dto in items)
                {
                    _ = new ItemType(
                        dto.Identifier,
                        dto.NetworkId,
                        maxStackSize: 64,
                        tags: null,
                        isComponentBased: dto.ComponentBased,
                        version: dto.ItemVersion,
                        properties: new CompoundTag());
                }
            }

            _ = ItemType.GetOrAir("minecraft:air");
            LoadCreativeItems(path);
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

    static void LoadCreativeItems(string dataRoot)
    {
        string path = Path.Combine(dataRoot, "creative_content.json");
        if (!File.Exists(path))
        {
            _creativeItems = [];
            return;
        }

        List<CreativeContentDto>? entries = JsonSerializer.Deserialize<List<CreativeContentDto>>(File.ReadAllBytes(path), JsonOptions);
        Dictionary<uint, ItemStack> creative = [];
        if (entries is not null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                CreativeContentDto entry = entries[i];
                ItemType? type = ItemType.GetByNetwork((int)entry.NetworkId);
                if (type is not null)
                {
                    creative[(uint)entry.NetworkId] = new ItemStack(type, 1);
                }
            }
        }

        _creativeItems = creative;
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
    }

    private sealed class CreativeContentDto
    {
        public int NetworkId { get; set; }
        public int GroupIndex { get; set; }
    }
}
