using System.Text.Json;
using Orion.Protocol.Nbt;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Registry;

/// <summary>
/// Minimal curated item registry (5 vanilla blocks). Creative lists those blocks; commands payload is empty.
/// </summary>
public static class CuratedItemCatalog
{
    private static readonly object InitLock = new();
    private static bool _initialized;
    private static byte[]? _registryPayload;
    private static byte[]? _creativePayload;
    private static byte[]? _actorIdentifiersPayload;
    private static byte[]? _commandsPayload;
    private static readonly Dictionary<string, CuratedItem> ItemsByIdentifier = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, CuratedItem> ItemsByNetworkId = new();

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
                CuratedItem item = new(
                    dto.NetworkId,
                    dto.Identifier,
                    dto.IsBlock,
                    dto.BlockStateHash,
                    dto.ComponentBased,
                    dto.ItemVersion,
                    Convert.FromBase64String(dto.PropertiesBase64));

                ItemsByIdentifier[item.Identifier] = item;
                ItemsByNetworkId[item.NetworkId] = item;
            }

            _registryPayload = SerializePacketBody(BuildItemRegistryPacket(items));
            _creativePayload = SerializePacketBody(BuildCreativeContentPacket(groups, content));
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

    private static ItemRegistryPacket BuildItemRegistryPacket(List<CuratedItemDto> items)
    {
        ItemRegistryPacket packet = new();
        foreach (CuratedItemDto dto in items)
        {
            packet.Items.Add(new ItemEntry
            {
                Name = dto.Identifier,
                RuntimeId = checked((short)dto.NetworkId),
                ComponentBased = dto.ComponentBased,
                Version = Math.Max(2, dto.ItemVersion),
                Data = new CompoundTag()
            });
        }

        return packet;
    }

    private static CreativeContentPacket BuildCreativeContentPacket(
        List<CreativeGroupDto> groups,
        List<CreativeContentDto> content)
    {
        CreativeContentPacket packet = new();

        foreach (CreativeGroupDto group in groups)
        {
            if (!ItemsByNetworkId.TryGetValue(group.IconNetworkId, out CuratedItem icon))
            {
                throw new InvalidDataException($"Creative group icon network id {group.IconNetworkId} is not registered.");
            }

            packet.Groups.Add(new CreativeGroup
            {
                Category = group.Category,
                Name = group.Name,
                Icon = BuildCreativeDescriptor(icon)
            });
        }

        for (int index = 0; index < content.Count; index++)
        {
            CreativeContentDto entry = content[index];
            if (!ItemsByNetworkId.TryGetValue(entry.NetworkId, out CuratedItem item))
            {
                throw new InvalidDataException($"Creative content network id {entry.NetworkId} is not registered.");
            }

            CreativeItem creativeItem = new()
            {
                ItemIndex = index,
                GroupIndex = entry.GroupIndex
            };

            if (!string.IsNullOrEmpty(entry.InstanceBase64))
            {
                creativeItem.ItemInstance.RawData = Convert.FromBase64String(entry.InstanceBase64);
            }
            else
            {
                creativeItem.ItemInstance = BuildCreativeDescriptor(item);
            }

            packet.Items.Add(creativeItem);
        }

        return packet;
    }

    private static CreativeItemInstanceDescriptor BuildCreativeDescriptor(CuratedItem item)
    {
        return new CreativeItemInstanceDescriptor
        {
            NetworkId = item.NetworkId,
            StackSize = 1,
            Metadata = 0,
            NetworkBlockId = item.IsBlock ? item.BlockStateHash : 0
        };
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
        public string PropertiesBase64 { get; set; } = "CgAAAA==";
    }

    private sealed class CreativeGroupDto
    {
        public int Category { get; set; }
        public string Name { get; set; } = "";
        public int IconNetworkId { get; set; }
    }

    private sealed class CreativeContentDto
    {
        public int NetworkId { get; set; }
        public int GroupIndex { get; set; }
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
    byte[] PropertiesNbt);
