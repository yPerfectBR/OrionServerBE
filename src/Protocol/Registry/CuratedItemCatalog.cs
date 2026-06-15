using System.Text.Json;
using Orion.Protocol.Io;
using Orion.Protocol.Nbt;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace Orion.Protocol.Registry;

/// <summary>
/// Minimal curated item registry (5 vanilla blocks). Creative menu uses items flagged creative in items.json.
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

            string root = ResolveDataRoot();
            List<CuratedItemDto> items = LoadJson<List<CuratedItemDto>>(Path.Combine(root, "items.json"));
            List<CreativeGroupDto> groups = LoadJson<List<CreativeGroupDto>>(Path.Combine(root, "creative_groups.json"));
            List<CreativeContentDto> content = LoadJson<List<CreativeContentDto>>(Path.Combine(root, "creative_content.json"));

            ItemsByIdentifier.Clear();
            ItemsByNetworkId.Clear();
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
            }

            _registryPayload = SerializePacketBody(BuildItemRegistryPacket(items));
            _creativePayload = SerializePacketBody(BuildCreativeContentPacket(items, groups, content));
            _actorIdentifiersPayload = SerializePacketBody(new AvailableActorIdentifiersPacket { Data = new CompoundTag() });
            _commandsPayload = SerializePacketBody(new AvailableCommandsPacket());
            _initialized = true;
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

    public static IReadOnlyCollection<string> GetRegisteredIdentifiers() => ItemsByIdentifier.Keys;

    public static bool TryGetCreativeMenuItem(int index, out CuratedItem item)
    {
        EnsureInitialized();
        if (index < 0 || index >= CreativeMenuItems.Count)
        {
            item = default;
            return false;
        }

        item = CreativeMenuItems[index];
        return true;
    }

    private static ItemRegistryPacket BuildItemRegistryPacket(List<CuratedItemDto> items)
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

    private static CreativeContentPacket BuildCreativeContentPacket(
        List<CuratedItemDto> items,
        List<CreativeGroupDto> groups,
        List<CreativeContentDto> contentOverrides)
    {
        CreativeContentPacket packet = new();
        CreativeMenuItems.Clear();

        foreach (CreativeGroupDto group in groups)
        {
            string iconIdentifier = !string.IsNullOrWhiteSpace(group.Icon)
                ? group.Icon
                : group.IconNetworkId is not 0 && ItemsByNetworkId.TryGetValue(group.IconNetworkId, out CuratedItem iconByNetwork)
                    ? iconByNetwork.Identifier
                    : throw new InvalidDataException($"Creative group '{group.Name}' has no icon.");

            if (!ItemsByIdentifier.TryGetValue(iconIdentifier, out CuratedItem icon))
            {
                throw new InvalidDataException($"Creative group icon '{iconIdentifier}' is not registered.");
            }

            packet.Groups.Add(new CreativeGroup
            {
                Category = group.Category,
                Name = group.Name,
                Icon = BuildGroupIcon(icon)
            });
        }

        int creativeIndex = 0;
        foreach (CreativeContentDto contentEntry in contentOverrides)
        {
            CuratedItem item = ResolveCreativeContentItem(contentEntry);

            packet.Items.Add(new CreativeItem
            {
                ItemIndex = creativeIndex,
                GroupIndex = contentEntry.GroupIndex,
                ItemInstance = ReadCreativeDescriptor(contentEntry, item)
            });

            CreativeMenuItems.Add(item);
            creativeIndex++;
        }

        return packet;
    }

    private static CuratedItem ResolveCreativeContentItem(CreativeContentDto entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Type))
        {
            if (!ItemsByIdentifier.TryGetValue(entry.Type, out CuratedItem byType))
            {
                throw new InvalidDataException($"Creative content type '{entry.Type}' is not registered.");
            }

            return byType;
        }

        if (entry.NetworkId != 0 && ItemsByNetworkId.TryGetValue(entry.NetworkId, out CuratedItem byNetwork))
        {
            return byNetwork;
        }

        throw new InvalidDataException("Creative content entry must specify type or networkId.");
    }

    private static CreativeItemInstanceDescriptor ReadCreativeDescriptor(CreativeContentDto entry, CuratedItem item)
    {
        CreativeItemInstanceDescriptor parsed = new();
        string? instance = entry.Instance ?? entry.InstanceBase64;
        if (!string.IsNullOrWhiteSpace(instance))
        {
            byte[] raw = Convert.FromBase64String(instance);
            int offset = 0;
            BinaryReader reader = new(raw, ref offset);
            parsed.Read(reader);
        }
        else
        {
            return BuildCreativeDescriptor(item);
        }

        return new CreativeItemInstanceDescriptor
        {
            NetworkId = item.NetworkId,
            StackSize = parsed.StackSize == 0 ? (ushort)1 : parsed.StackSize,
            Metadata = parsed.Metadata,
            NetworkBlockId = item.IsBlock ? item.BlockStateHash : 0,
            ExtraData = parsed.ExtraData
        };
    }

    private static CreativeItemInstanceDescriptor BuildGroupIcon(CuratedItem item)
    {
        return new CreativeItemInstanceDescriptor
        {
            NetworkId = item.NetworkId,
            StackSize = 1,
            Metadata = 0,
            NetworkBlockId = item.IsBlock ? item.BlockStateHash : 0
        };
    }

    private static CreativeItemInstanceDescriptor BuildCreativeDescriptor(CuratedItem item)
    {
        return new CreativeItemInstanceDescriptor
        {
            NetworkId = item.NetworkId,
            StackSize = 1,
            Metadata = 0,
            NetworkBlockId = item.IsBlock ? item.BlockStateHash : 0,
            ExtraData = EmptyItemInstanceUserData
        };
    }

    /// <summary>
    /// Bedrock expects a 10-byte empty user-data block (marker + empty canPlaceOn/canDestroy),
    /// not a zero-length extras section.
    /// </summary>
    private static readonly ItemInstanceUserData EmptyItemInstanceUserData = new();

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
        byte[] buffer = new byte[65536];
        int offset = 0;
        BinaryWriter writer = new(buffer, ref offset);
        packet.Serialize(writer);
        return buffer[..offset];
    }

    private static string ResolveDataRoot()
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

        throw new FileNotFoundException("Could not locate Protocol/Data/orion/items.json");
    }

    private static T LoadJson<T>(string path)
    {
        return JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), JsonOptions)
            ?? throw new InvalidDataException($"Failed to parse {path}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    private sealed class CreativeGroupDto
    {
        public int Category { get; set; }
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public int IconNetworkId { get; set; }
    }

    private sealed class CreativeContentDto
    {
        public string Type { get; set; } = "";
        public int NetworkId { get; set; }
        public int GroupIndex { get; set; }
        public string? Instance { get; set; }
        public string? InstanceBase64 { get; set; }
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
