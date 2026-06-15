using System.Buffers;
using Orion.Config;
using WorldLogger = Orion.Logger.Logger;
using Orion.Protocol.Enums;
using Orion.World.Provider.Storage;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Provider;

internal sealed class ChunkStore
{
    private readonly IWorldDatabase _database;

    public ChunkStore(IWorldDatabase database) => _database = database;

    public bool Exists(DimensionType dimensionType, int x, int z) => ReadBytes(dimensionType, x, z) is not null;

    public ChunkColumn? Load(DimensionType dimensionType, int x, int z)
    {
        byte[]? terrain = ReadBytes(dimensionType, x, z);
        if (terrain is null)
        {
            terrain = _database.Get(LevelDbKeyBuilder.BuildChunkKey(x, z));
            if (terrain is null || terrain.Length == 0)
            {
                return null;
            }
        }

        ChunkColumn? chunk = DecodeChunk(terrain, dimensionType, x, z);
        if (chunk is null)
        {
            return null;
        }

        chunk.Dirty = false;
        return chunk;
    }

    public void Save(WorldWriteBatch batch, ChunkColumn chunk)
    {
        byte[] terrain = WriteChunkPayload(chunk);
        batch.Put(LevelDbKeyBuilder.BuildChunkKey(chunk.Type, chunk.X, chunk.Z), terrain);
    }

    public void Delete(WorldWriteBatch batch, DimensionType dimensionType, int x, int z)
    {
        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockStorageListKey(dimensionType, x, z));
        batch.Delete(LevelDbKeyBuilder.BuildChunkKey(x, z));
        batch.Delete(LevelDbKeyBuilder.BuildBlockStorageListKey(x, z));
    }

    private byte[]? ReadBytes(DimensionType dimensionType, int x, int z)
    {
        byte[]? data = _database.Get(LevelDbKeyBuilder.BuildChunkKey(dimensionType, x, z));
        return data is { Length: > 0 } ? data : null;
    }

    private static ChunkColumn? DecodeChunk(byte[] terrain, DimensionType dimensionType, int x, int z)
    {
        int offset = 0;
        BinaryReader reader = new(terrain, ref offset);
        try
        {
            return ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true);
        }
        catch (Exception namedBiomeException)
        {
            offset = 0;
            reader = new(terrain, ref offset);
            try
            {
                return ChunkColumn.Deserialize(dimensionType, x, z, reader, nbt: true, biomeNbt: false);
            }
            catch
            {
                offset = 0;
                reader = new(terrain, ref offset);
                try
                {
                    return ChunkColumn.Deserialize(dimensionType, x, z, reader);
                }
                catch
                {
                    WorldLogger.Warn(LogCategory.World, "Failed loading chunk {0},{1} in {2}: {3}", x, z, dimensionType, namedBiomeException.Message);
                    return null;
                }
            }
        }
    }

    private static byte[] WriteChunkPayload(ChunkColumn chunk)
    {
        int size = 2 * 1024 * 1024;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

        while (true)
        {
            int offset = 0;
            BinaryWriter writer = new(buffer, ref offset);

            try
            {
                ChunkColumn.Serialize(chunk, writer, nbt: true);
                byte[] data = writer.GetProcessedBytes().ToArray();
                ArrayPool<byte>.Shared.Return(buffer);
                return data;
            }
            catch (Exception exception) when (
                exception is ArgumentOutOfRangeException or IndexOutOfRangeException)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                size <<= 1;
                if (size > 64 * 1024 * 1024)
                {
                    throw;
                }

                buffer = ArrayPool<byte>.Shared.Rent(size);
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(buffer);
                throw;
            }
        }
    }
}
