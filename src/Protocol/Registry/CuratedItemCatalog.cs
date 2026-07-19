using System.Text.Json;
using Orion.Protocol.Io;
using Orion.Protocol.Nbt;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace Orion.Protocol.Registry;

/// <summary>
/// Builds ItemRegistry + CreativeContent payloads.
/// ItemRegistry uses the full vanilla palette (<c>item_types.json</c>) so the client
/// does not replace its item table with a tiny set. CreativeContent uses Orion Nature
/// blocks from <c>orion/items.json</c> plus optional in-memory registrations from
/// C# plugins via <see cref="RegisterCreativeTabEntries"/>.
/// </summary>
public static class CuratedItemCatalog
{
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);
    private static readonly object InitLock = new();

    private static bool _initialized;
    private static byte[]? _registryPayload;
    private static byte[]? _creativePayload;
    private static byte[]? _actorIdentifiersPayload;
    private static byte[]? _commandsPayload;
    private static readonly Dictionary<string, CuratedItem> ItemsByIdentifier = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, CuratedItem> ItemsByNetworkId = new();
    private static readonly List<CuratedItem> CreativeMenuItems = [];
    private static readonly HashSet<string> AllowlistedIdentifiers = new(StringComparer.Ordinal);
    private static readonly List<string> LoadedCreativePlugins = [];
    private static readonly List<(string PluginId, int Category, string Identifier)> PendingTabEntries = [];
    private static readonly List<string> PendingAllowlistIdentifiers = [];
    private static bool _nonNatureTabsEmpty = true;
    private static string _source = "uninitialized";

    public static bool IsInitialized
    {
        get
        {
            lock (InitLock)
            {
                return _initialized;
            }
        }
    }

    public static string Source
    {
        get
        {
            EnsureInitialized();
            return _source;
        }
    }

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }

            ItemsByIdentifier.Clear();
            ItemsByNetworkId.Clear();
            CreativeMenuItems.Clear();
            AllowlistedIdentifiers.Clear();
            // PendingTabEntries + LoadedCreativePlugins survive until ResetForTests /
            // RegisterCreativeTabEntries — they are filled by plugins before init.

            if (TryResolveVanillaRoot(out string vanillaRoot))
            {
                LoadVanillaPalette(vanillaRoot);
                string orionRoot = ResolveOrionRoot();
                ApplyOrionCreativeMenu(orionRoot);
                _source = "vanilla+orion:" + vanillaRoot;
            }
            else
            {
                LoadCuratedFallback(ResolveOrionRoot());
                _source = "orion-fallback";
            }

            _actorIdentifiersPayload = SerializePacketBody(new AvailableActorIdentifiersPacket { Data = new CompoundTag() });
            _commandsPayload = SerializePacketBody(new AvailableCommandsPacket());
            _initialized = true;
        }
    }

    /// <summary>
    /// Registers creative-tab fillers from a C# plugin. Must be called before the catalog
    /// initializes (load plugins before <see cref="EnsureInitialized"/>). Category 2 (Nature)
    /// is reserved for Orion world blocks and is ignored.
    /// </summary>
    public static void RegisterCreativeTabEntries(string pluginId, params (int Category, string Identifier)[] entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentNullException.ThrowIfNull(entries);

        lock (InitLock)
        {
            if (_initialized)
            {
                throw new InvalidOperationException(
                    "Creative tab entries must be registered before CuratedItemCatalog initializes. " +
                    "Load plugins before ItemRegistry / catalog bootstrap.");
            }

            if (!LoadedCreativePlugins.Contains(pluginId, StringComparer.Ordinal))
            {
                LoadedCreativePlugins.Add(pluginId);
            }

            foreach ((int category, string identifier) in entries)
            {
                if (category is < 1 or > 4 || category == 2 || string.IsNullOrWhiteSpace(identifier))
                {
                    continue;
                }

                PendingTabEntries.Add((pluginId, category, identifier));
            }
        }
    }

    /// <summary>
    /// Adds identifiers to the /give allowlist before catalog init (plugin item registration).
    /// </summary>
    public static void RegisterAllowlistedIdentifiers(string pluginId, params string[] identifiers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);
        ArgumentNullException.ThrowIfNull(identifiers);

        lock (InitLock)
        {
            if (_initialized)
            {
                throw new InvalidOperationException(
                    "Allowlisted identifiers must be registered before CuratedItemCatalog initializes.");
            }

            if (!LoadedCreativePlugins.Contains(pluginId, StringComparer.Ordinal))
            {
                LoadedCreativePlugins.Add(pluginId);
            }

            foreach (string identifier in identifiers)
            {
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    continue;
                }

                PendingAllowlistIdentifiers.Add(identifier);
            }
        }
    }

    /// <summary>
    /// True when Construction / Equipment / Items have no entries (Nature may still be filled).
    /// Bedrock often renders the whole creative menu empty in that case.
    /// </summary>
    public static bool NonNatureCreativeTabsEmpty
    {
        get
        {
            EnsureInitialized();
            return _nonNatureTabsEmpty;
        }
    }

    /// <summary>
    /// Test helper: clears catalog state so registrations can be exercised in isolation.
    /// </summary>
    internal static void ResetForTests()
    {
        lock (InitLock)
        {
            _initialized = false;
            _registryPayload = null;
            _creativePayload = null;
            _actorIdentifiersPayload = null;
            _commandsPayload = null;
            ItemsByIdentifier.Clear();
            ItemsByNetworkId.Clear();
            CreativeMenuItems.Clear();
            AllowlistedIdentifiers.Clear();
            LoadedCreativePlugins.Clear();
            PendingTabEntries.Clear();
            PendingAllowlistIdentifiers.Clear();
            _nonNatureTabsEmpty = true;
            _source = "uninitialized";
        }
    }

    public static byte[] GetItemRegistryPayload()
    {
        EnsureInitialized();
        return _registryPayload!;
    }

    public static byte[] GetCreativeContentPayload()
    {
        EnsureInitialized();
        return _creativePayload!;
    }

    public static byte[] GetAvailableActorIdentifiersPayload()
    {
        EnsureInitialized();
        return _actorIdentifiersPayload!;
    }

    public static byte[] GetAvailableCommandsPayload()
    {
        EnsureInitialized();
        return _commandsPayload!;
    }

    public static bool TryGetByIdentifier(string identifier, out CuratedItem item) =>
        ItemsByIdentifier.TryGetValue(identifier, out item!);

    public static bool TryGetByNetworkId(int networkId, out CuratedItem item) =>
        ItemsByNetworkId.TryGetValue(networkId, out item!);

    public static IReadOnlyCollection<string> GetRegisteredIdentifiers()
    {
        EnsureInitialized();
        return ItemsByIdentifier.Keys;
    }

    public static IReadOnlyList<CuratedItem> GetCreativeMenuItems()
    {
        EnsureInitialized();
        return CreativeMenuItems;
    }

    /// <summary>
    /// Identifiers allowed for /give (Orion curated set). Empty only before init.
    /// </summary>
    public static IReadOnlyCollection<string> GetAllowlistedIdentifiers()
    {
        EnsureInitialized();
        return AllowlistedIdentifiers;
    }

    /// <summary>
    /// Plugin ids that registered creative tab entries via <see cref="RegisterCreativeTabEntries"/>.
    /// </summary>
    public static IReadOnlyList<string> GetLoadedCreativePlugins()
    {
        EnsureInitialized();
        return LoadedCreativePlugins;
    }

    public static bool TryGetCreativeMenuItem(int creativeItemNetworkId, out CuratedItem item)
    {
        EnsureInitialized();
        int index = creativeItemNetworkId - 1;
        if (index < 0 || index >= CreativeMenuItems.Count)
        {
            item = default;
            return false;
        }

        item = CreativeMenuItems[index];
        return true;
    }

    /// <summary>
    /// Loads the full vanilla ItemRegistry palette (client-visible item table).
    /// Does not populate the creative menu — that comes from <see cref="ApplyOrionCreativeMenu"/>.
    /// </summary>
    private static void LoadVanillaPalette(string root)
    {
        Dictionary<string, int> blockHashes = LoadBlockDefaultHashes(root);
        List<ItemTypeDto> types = LoadJson<List<ItemTypeDto>>(Path.Combine(root, "item_types.json"));

        ItemRegistryPacket registry = new();
        bool hasAir = false;
        foreach (ItemTypeDto entry in types)
        {
            if (string.IsNullOrEmpty(entry.Identifier) || entry.NetworkId is null)
            {
                continue;
            }

            if (string.Equals(entry.Identifier, "minecraft:air", StringComparison.Ordinal) || entry.NetworkId == 0)
            {
                hasAir = true;
            }

            int blockHash = blockHashes.GetValueOrDefault(entry.Identifier);
            bool isBlock = blockHash != 0 || blockHashes.ContainsKey(entry.Identifier);
            CompoundTag properties = BuildProperties(entry.PropertiesPayload);

            CuratedItem item = new(
                entry.NetworkId.Value,
                entry.Identifier,
                isBlock,
                blockHash,
                entry.ComponentBased,
                entry.ItemVersion,
                properties);

            ItemsByIdentifier[item.Identifier] = item;
            ItemsByNetworkId[item.NetworkId] = item;

            registry.Items.Add(new ItemEntry
            {
                Name = item.Identifier,
                RuntimeId = checked((short)item.NetworkId),
                ComponentBased = item.ComponentBased,
                Version = Math.Max(1, item.ItemVersion),
                Data = item.PropertiesNbt
            });
        }

        if (!hasAir)
        {
            CuratedItem air = new(0, "minecraft:air", false, 0, true, 1, new CompoundTag());
            ItemsByIdentifier[air.Identifier] = air;
            ItemsByNetworkId[0] = air;
            registry.Items.Insert(0, new ItemEntry
            {
                Name = air.Identifier,
                RuntimeId = 0,
                ComponentBased = true,
                Version = 1,
                Data = air.PropertiesNbt
            });
        }

        _registryPayload = SerializePacketBody(registry);
    }

    /// <summary>
    /// Builds CreativeContent + allowlist from <c>orion/</c>, resolving item data from the
    /// already-loaded vanilla palette (correct network ids / block hashes).
    /// </summary>
    private static void ApplyOrionCreativeMenu(string orionRoot)
    {
        List<CuratedItemDto> items = LoadJson<List<CuratedItemDto>>(Path.Combine(orionRoot, "items.json"));

        foreach (string pendingId in PendingAllowlistIdentifiers)
        {
            AllowlistedIdentifiers.Add(pendingId);
        }

        List<CuratedItem> creativeItems = [];
        foreach (CuratedItemDto dto in items)
        {
            AllowlistedIdentifiers.Add(dto.Identifier);

            // Prefer vanilla palette entry; overlay Orion block-hash / flags when present.
            if (ItemsByIdentifier.TryGetValue(dto.Identifier, out CuratedItem existing))
            {
                CuratedItem merged = new(
                    existing.NetworkId,
                    existing.Identifier,
                    dto.IsBlock || existing.IsBlock,
                    dto.BlockStateHash != 0 ? dto.BlockStateHash : existing.BlockStateHash,
                    existing.ComponentBased,
                    existing.ItemVersion,
                    existing.PropertiesNbt);
                ItemsByIdentifier[merged.Identifier] = merged;
                ItemsByNetworkId[merged.NetworkId] = merged;
                if (dto.Creative)
                {
                    creativeItems.Add(merged);
                }

                continue;
            }

            CompoundTag properties = ReadPropertiesNbt(dto.PropertiesBase64);
            CuratedItem item = new(
                dto.NetworkId,
                dto.Identifier,
                dto.IsBlock,
                dto.BlockStateHash,
                dto.ComponentBased,
                dto.ItemVersion,
                properties);
            ItemsByIdentifier[item.Identifier] = item;
            ItemsByNetworkId[item.NetworkId] = item;
            if (dto.Creative)
            {
                creativeItems.Add(item);
            }
        }

        _creativePayload = SerializePacketBody(BuildCreativePacket(creativeItems, ResolveRegisteredTabItems()));
    }

    private static void LoadCuratedFallback(string root)
    {
        List<CuratedItemDto> items = LoadJson<List<CuratedItemDto>>(Path.Combine(root, "items.json"));

        foreach (string pendingId in PendingAllowlistIdentifiers)
        {
            AllowlistedIdentifiers.Add(pendingId);
        }

        List<CuratedItem> creativeItems = [];
        foreach (CuratedItemDto dto in items)
        {
            CompoundTag properties = ReadPropertiesNbt(dto.PropertiesBase64);
            CuratedItem item = new(
                dto.NetworkId,
                dto.Identifier,
                dto.IsBlock,
                dto.BlockStateHash,
                dto.ComponentBased,
                dto.ItemVersion,
                properties);

            ItemsByIdentifier[item.Identifier] = item;
            ItemsByNetworkId[item.NetworkId] = item;
            AllowlistedIdentifiers.Add(item.Identifier);
            if (dto.Creative)
            {
                creativeItems.Add(item);
            }
        }

        _registryPayload = SerializePacketBody(BuildCuratedRegistryPacket(items));
        _creativePayload = SerializePacketBody(BuildCreativePacket(creativeItems, ResolveRegisteredTabItems()));
    }

    private static ItemRegistryPacket BuildCuratedRegistryPacket(List<CuratedItemDto> items)
    {
        ItemRegistryPacket packet = new();
        foreach (CuratedItemDto dto in items)
        {
            if (!ItemsByIdentifier.TryGetValue(dto.Identifier, out CuratedItem curated))
            {
                continue;
            }

            packet.Items.Add(new ItemEntry
            {
                Name = dto.Identifier,
                RuntimeId = checked((short)dto.NetworkId),
                ComponentBased = dto.ComponentBased,
                Version = Math.Max(2, dto.ItemVersion),
                Data = curated.PropertiesNbt
            });
        }

        return packet;
    }

    /// <summary>
    /// Bedrock creative UI needs at least one item in each used category tab or the inventory
    /// can render empty. Orion world blocks live in Nature; other tabs come from C# plugins
    /// via <see cref="RegisterCreativeTabEntries"/> (see sample MinimalInventoryItems).
    /// </summary>
    private static CreativeContentPacket BuildCreativePacket(
        List<CuratedItem> natureItems,
        List<(int Category, CuratedItem Item)> pluginTabItems)
    {
        CreativeContentPacket packet = new();
        CreativeMenuItems.Clear();

        CuratedItem airIcon = ItemsByIdentifier.TryGetValue("minecraft:air", out CuratedItem air)
            ? air
            : new CuratedItem(0, "minecraft:air", false, 0, true, 1, new CompoundTag());

        CreativeItemInstanceDescriptor AnonymousAirIcon() => BuildCreativeDescriptor(airIcon);

        // Group indices: 0 Construction, 1 Nature, 2 Equipment, 3 Items
        int[] categories = [1, 2, 3, 4];
        foreach (int category in categories)
        {
            packet.Groups.Add(new CreativeGroup
            {
                Category = category,
                Name = string.Empty,
                Icon = AnonymousAirIcon()
            });
        }

        uint creativeItemNetworkId = 1;
        bool hasConstruction = false;
        bool hasEquipment = false;
        bool hasItems = false;

        void AddEntry(CuratedItem item, uint groupIndex)
        {
            packet.Items.Add(new CreativeItem
            {
                CreativeItemNetworkId = creativeItemNetworkId++,
                GroupIndex = groupIndex,
                ItemInstance = BuildCreativeDescriptor(item)
            });
            CreativeMenuItems.Add(item);
            AllowlistedIdentifiers.Add(item.Identifier);

            switch (groupIndex)
            {
                case 0:
                    hasConstruction = true;
                    break;
                case 2:
                    hasEquipment = true;
                    break;
                case 3:
                    hasItems = true;
                    break;
            }
        }

        foreach ((int category, CuratedItem item) in pluginTabItems.Where(e => e.Category == 1))
        {
            AddEntry(item, 0);
        }

        foreach (CuratedItem item in natureItems)
        {
            AddEntry(item, 1);
        }

        foreach ((int category, CuratedItem item) in pluginTabItems.Where(e => e.Category == 3))
        {
            AddEntry(item, 2);
        }

        foreach ((int category, CuratedItem item) in pluginTabItems.Where(e => e.Category == 4))
        {
            AddEntry(item, 3);
        }

        _nonNatureTabsEmpty = !hasConstruction || !hasEquipment || !hasItems;
        return packet;
    }

    private static List<(int Category, CuratedItem Item)> ResolveRegisteredTabItems()
    {
        List<(int Category, CuratedItem Item)> result = [];
        foreach ((string pluginId, int category, string identifier) in PendingTabEntries)
        {
            _ = pluginId;
            if (!ItemsByIdentifier.TryGetValue(identifier, out CuratedItem item))
            {
                continue;
            }

            result.Add((category, item));
        }

        return result;
    }

    private static CreativeItemInstanceDescriptor BuildCreativeDescriptor(CuratedItem item) =>
        new()
        {
            NetworkId = item.NetworkId,
            StackSize = 1,
            Metadata = 0,
            NetworkBlockId = item.IsBlock ? item.BlockStateHash : 0
        };

    private static CompoundTag BuildProperties(JsonElement? payload)
    {
        if (payload is not { ValueKind: JsonValueKind.Object } element)
        {
            return new CompoundTag();
        }

        CompoundTag properties = ToCompoundTag(element);
        SerializeComponents(properties);
        return properties;
    }

    private static void SerializeComponents(CompoundTag properties)
    {
        if (properties.Get<ListTag>("components") is not ListTag componentList)
        {
            return;
        }

        CompoundTag components = new();
        CompoundTag itemProperties = new();

        if (properties.Get<CompoundTag>("icon") is CompoundTag iconTag)
        {
            itemProperties.Set("minecraft:icon", iconTag);
        }

        if (properties.Get<IntTag>("maxAmount") is IntTag maxStack)
        {
            itemProperties.Set("max_stack_size", new IntTag { Value = maxStack.Value });
        }

        if (properties.Get<IntTag>("damage") is IntTag damage)
        {
            itemProperties.Set("damage", damage);
        }

        if (properties.Get<IntTag>("useDuration") is IntTag useDuration)
        {
            itemProperties.Set("use_duration", useDuration);
        }

        if (itemProperties.Values.Count > 0)
        {
            components.Set("item_properties", itemProperties);
        }

        for (int i = 0; i < componentList.Values.Count; i++)
        {
            if (componentList.Values[i] is not StringTag component || string.IsNullOrWhiteSpace(component.Value))
            {
                continue;
            }

            string identifier = component.Value;
            string payloadKey = identifier.StartsWith("minecraft:", StringComparison.Ordinal)
                ? identifier["minecraft:".Length..]
                : identifier;

            CompoundTag componentPayload = properties.Get<CompoundTag>(payloadKey) ?? new CompoundTag();
            components.Set(identifier, componentPayload);
        }

        properties.Set("components", components);
    }

    private static CompoundTag ToCompoundTag(JsonElement element)
    {
        CompoundTag tag = new();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            BaseTag? value = ToNbtTag(property.Value);
            if (value is not null)
            {
                tag.Set(property.Name, value);
            }
        }

        return tag;
    }

    private static BaseTag? ToNbtTag(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => ToCompoundTag(element),
        JsonValueKind.Array => ToListTag(element),
        JsonValueKind.String => new StringTag { Value = element.GetString() ?? string.Empty },
        JsonValueKind.Number => ToNumberTag(element),
        JsonValueKind.True => new ByteTag { Value = 1 },
        JsonValueKind.False => new ByteTag { Value = 0 },
        _ => null
    };

    private static ListTag ToListTag(JsonElement element)
    {
        ListTag tag = new();
        foreach (JsonElement item in element.EnumerateArray())
        {
            BaseTag? value = ToNbtTag(item);
            if (value is not null)
            {
                tag.Values.Add(value);
            }
        }

        return tag;
    }

    private static BaseTag ToNumberTag(JsonElement element) =>
        element.TryGetInt32(out int value)
            ? new IntTag { Value = value }
            : new FloatTag { Value = element.GetSingle() };

    private static CompoundTag ReadPropertiesNbt(string propertiesBase64)
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
        BinaryReader reader = new(payload, ref offset);
        return NBT.ReadTag<CompoundTag>(reader, NetworkNbtOptions);
    }

    private static byte[] SerializePacketBody(DataPacket packet)
    {
        int size = 65_536;
        while (true)
        {
            byte[] buffer = new byte[size];
            try
            {
                int offset = 0;
                BinaryWriter writer = new(buffer, ref offset);
                packet.Serialize(writer);
                return buffer[..offset];
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                size = checked(size * 2);
            }
        }
    }

    private static Dictionary<string, int> LoadBlockDefaultHashes(string root)
    {
        string path = Path.Combine(root, "block_default_hashes.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllBytes(path), JsonOptions)
            ?? new Dictionary<string, int>(StringComparer.Ordinal);
    }

    private static bool TryResolveVanillaRoot(out string root)
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Protocol", "Data"),
            Path.Combine(AppContext.BaseDirectory, "Data"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "Protocol", "Data"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Protocol", "Data"))
        ];

        foreach (string path in candidates)
        {
            if (File.Exists(Path.Combine(path, "item_types.json")))
            {
                root = path;
                return true;
            }
        }

        root = "";
        return false;
    }

    private static string ResolveOrionRoot()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "Protocol", "Data", "orion"),
            Path.Combine(AppContext.BaseDirectory, "Data", "orion"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "orion"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Protocol", "Data", "orion"))
        ];

        foreach (string path in candidates)
        {
            if (File.Exists(Path.Combine(path, "items.json")))
            {
                return path;
            }
        }

        throw new FileNotFoundException("Could not locate item_types.json or Protocol/Data/orion/items.json");
    }

    private static T LoadJson<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), JsonOptions)
        ?? throw new InvalidDataException($"Failed to parse {path}");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ItemTypeDto
    {
        public string Identifier { get; set; } = "";
        public List<string> Tags { get; set; } = [];
        public int MaxAmount { get; set; } = 64;
        public bool ComponentBased { get; set; }
        public int? NetworkId { get; set; }
        public int ItemVersion { get; set; } = 1;
        public JsonElement? PropertiesPayload { get; set; }
    }

    private sealed class CuratedItemDto
    {
        public int NetworkId { get; set; }
        public string Identifier { get; set; } = "";
        public bool IsBlock { get; set; }
        public int BlockStateHash { get; set; }
        public bool ComponentBased { get; set; }
        public int ItemVersion { get; set; } = 2;
        public bool Creative { get; set; } = true;
        public string PropertiesBase64 { get; set; } = "CgAAAA==";
    }

}

public readonly record struct CuratedItem(
    int NetworkId,
    string Identifier,
    bool IsBlock,
    int BlockStateHash,
    bool ComponentBased,
    int ItemVersion,
    CompoundTag PropertiesNbt);
