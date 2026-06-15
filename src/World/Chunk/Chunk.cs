using Orion.World.Block;
using Orion.Protocol.Enums;
using Orion.Protocol.Io;
using Orion.Protocol.Nbt;
using Orion.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryStream = Basalt.Binary.BinaryStream;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.World.Chunk;

public sealed class Chunk
{
    public const int MaxSubChunks = 24;

    private readonly Dictionary<(int X, int Y, int Z), BlockLevelStorage> _blocks = [];

    public DimensionType Type { get; }
    public int X { get; }
    public int Z { get; }
    public long Hash { get; }
    public SubChunk?[] SubChunks { get; }

    public byte[]? Cache;
    public bool Dirty;

    public Chunk(int x, int z, DimensionType type, SubChunk?[]? subChunks = null)
    {
        X = x;
        Z = z;
        Type = type;
        Hash = ((long)x << 32) | (uint)z;
        SubChunks = subChunks ?? new SubChunk?[MaxSubChunks];
    }

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0)
    {
        SubChunk? subChunk = PeekSubChunk(y >> 4);
        if (subChunk is null)
        {
            return BlockPermutation.Air;
        }

        int state = subChunk.GetState(x & 0xF, y & 0xF, z & 0xF, layer);
        return BlockPermutation.Resolve(state);
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0, bool dirty = true)
    {
        SubChunk subChunk = GetSubChunk(y >> 4);
        subChunk.SetState(x & 0xF, y & 0xF, z & 0xF, permutation.NetworkId, layer);

        if (dirty)
        {
            Dirty = true;
        }

        Cache = null;
    }

    public int GetBiome(int x, int y, int z)
    {
        SubChunk? subChunk = PeekSubChunk(y >> 4);
        return subChunk?.GetBiome(x & 0xF, y & 0xF, z & 0xF) ?? 0;
    }

    public void SetBiome(int x, int y, int z, int biomeId, bool dirty = true)
    {
        SubChunk subChunk = GetSubChunk(y >> 4);
        subChunk.SetBiome(x & 0xF, y & 0xF, z & 0xF, biomeId);

        if (dirty)
        {
            Dirty = true;
        }

        Cache = null;
    }

    public int GetTopmostLevel(int x, int z, int startY)
    {
        for (int y = startY; y >= -64; y--)
        {
            if (!GetPermutation(x, y, z).IsAir)
            {
                return y;
            }
        }

        return -64;
    }

    public SubChunk GetSubChunk(int index)
    {
        int offset = Type == DimensionType.Overworld ? 4 : 0;

        if (index + offset < 0)
        {
            index = 0;
            offset = 0;
        }
        else if (index + offset >= MaxSubChunks)
        {
            index = MaxSubChunks - 1;
            offset = 0;
        }

        int resolved = index + offset;
        if (SubChunks[resolved] is null)
        {
            for (int i = 0; i <= resolved; i++)
            {
                if (SubChunks[i] is not null)
                {
                    continue;
                }

                SubChunk subChunk = new() { Index = (sbyte)(i - offset) };
                SubChunks[i] = subChunk;

                if (i == resolved)
                {
                    return subChunk;
                }
            }
        }

        return SubChunks[resolved]!;
    }

    public void SetSubChunk(int index, SubChunk subChunk)
    {
        int offset = Type == DimensionType.Overworld ? 4 : 0;

        if (index + offset < 0)
        {
            index = 0;
            offset = 0;
        }
        else if (index + offset >= MaxSubChunks)
        {
            index = MaxSubChunks - 1;
            offset = 0;
        }

        SubChunks[index + offset] = subChunk;
        Cache = null;
    }

    public int GetSubChunkSendCount()
    {
        int emptyTail = 0;
        for (int index = MaxSubChunks - 1; index >= 0; index--)
        {
            SubChunk? subChunk = SubChunks[index];
            if (subChunk is null || subChunk.IsEmpty())
            {
                emptyTail++;
                continue;
            }

            break;
        }

        return MaxSubChunks - emptyTail;
    }

    public List<BlockLevelStorage> GetAllBlockStorages() => [.. _blocks.Values];

    public BlockLevelStorage? GetBlockStorage(BlockPos position) =>
        _blocks.GetValueOrDefault((position.X, position.Y, position.Z));

    public void SetBlockStorage(BlockPos position, BlockLevelStorage? data, bool dirty = true)
    {
        var key = (position.X, position.Y, position.Z);

        if (data is null)
        {
            _blocks.Remove(key);
        }
        else
        {
            data.SetPosition(position);
            _blocks[key] = data;
        }

        if (dirty)
        {
            Dirty = true;
            Cache = null;
        }
    }

    public bool IsEmpty()
    {
        for (int i = 0; i < SubChunks.Length; i++)
        {
            SubChunk? subChunk = SubChunks[i];
            if (subChunk is not null && !subChunk.IsEmpty())
            {
                return false;
            }
        }

        return true;
    }

    public void ReleaseMemory()
    {
        Cache = null;
        _blocks.Clear();
        Array.Clear(SubChunks, 0, SubChunks.Length);
        Dirty = false;
    }

    public static byte[] Serialize(Chunk chunk, bool nbt = false)
    {
        if (!nbt && chunk.Cache is not null)
        {
            return chunk.Cache;
        }

        using BinaryStream stream = BinaryStream.Rent(2 * 1024 * 1024);
        Serialize(chunk, stream, nbt);
        byte[] serialized = stream.GetProcessedBytes().ToArray();
        if (nbt)
        {
            return serialized;
        }

        chunk.Cache = serialized;
        return chunk.Cache;
    }

    public static int Serialize(Chunk chunk, BinaryWriter writer, bool nbt = false)
    {
        int subChunkCount = chunk.GetSubChunkSendCount();
        for (int index = 0; index < subChunkCount; index++)
        {
            int offset = chunk.Type == DimensionType.Overworld ? 4 : 0;
            SubChunk? subChunk = chunk.SubChunks[index];

            if (subChunk is null)
            {
                subChunk = new SubChunk { Index = (sbyte)(index - offset) };
            }

            SubChunk.Serialize(subChunk, writer, nbt);
        }

        for (int index = 0; index < subChunkCount; index++)
        {
            SubChunk? subChunk = chunk.SubChunks[index];
            if (subChunk is null || subChunk.IsEmpty())
            {
                continue;
            }

            BiomeStorage.Serialize(subChunk.Biomes, ref writer, nbt);
        }

        writer.WriteUInt8(0);

        List<BlockLevelStorage> blockEntities = chunk.GetAllBlockStorages();
        for (int i = 0; i < blockEntities.Count; i++)
        {
            NBT.WriteTag(writer, blockEntities[i], new TagOptions(Name: true, Type: true, VarInt: false));
        }

        return writer.Offset;
    }

    public static Chunk Deserialize(
        DimensionType type,
        int x,
        int z,
        BinaryReader reader,
        bool nbt = false,
        bool? biomeNbt = null)
    {
        SubChunk?[] subChunks = new SubChunk?[MaxSubChunks];

        for (int index = 0; index < MaxSubChunks; index++)
        {
            if (reader.Remaining <= 0)
            {
                break;
            }

            byte header = reader.Buffer[reader.Offset];
            if (header != 8 && header != 9)
            {
                break;
            }

            subChunks[index] = SubChunk.Deserialize(reader, nbt);
        }

        for (int i = 0; i < subChunks.Length; i++)
        {
            SubChunk? subChunk = subChunks[i];
            if (subChunk is null || subChunk.IsEmpty())
            {
                continue;
            }

            subChunk.Biomes = BiomeStorage.Deserialize(ref reader, biomeNbt ?? nbt);
        }

        if (reader.Remaining > 0)
        {
            _ = reader.ReadUInt8();
        }

        Chunk chunk = new(x, z, type, subChunks);

        while (reader.Remaining > 0)
        {
            if ((TagType)reader.Buffer[reader.Offset] != TagType.Compound)
            {
                break;
            }

            CompoundTag tag = NBT.ReadTag<CompoundTag>(
                reader,
                new TagOptions(Name: true, Type: true, VarInt: false));

            BlockLevelStorage storage = new(chunk, tag);
            chunk.SetBlockStorage(storage.GetPosition(), storage, dirty: false);
        }

        chunk.Cache = nbt ? null : reader.GetProcessedBytes().ToArray();
        return chunk;
    }

    private SubChunk? PeekSubChunk(int index)
    {
        int offset = Type == DimensionType.Overworld ? 4 : 0;
        int resolved = index + offset;

        if (resolved < 0 || resolved >= MaxSubChunks)
        {
            return null;
        }

        return SubChunks[resolved];
    }
}
