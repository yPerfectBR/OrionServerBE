using Orion.Protocol.Enums;
using Orion.World.Provider.Storage;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Provider;

public sealed class LevelDbProvider : WorldProvider
{
    private readonly IWorldDatabase _database;
    private readonly ChunkStore _chunks;

    public override string Identifier => "leveldb";

    internal IWorldDatabase Database => _database;

    public LevelDbProvider(string path)
    {
        _database = new LevelDbDatabase(path);
        _chunks = new ChunkStore(_database);
    }

    internal LevelDbProvider(IWorldDatabase database)
    {
        _database = database;
        _chunks = new ChunkStore(_database);
    }

    public override bool HasChunk(DimensionType dimensionType, int x, int z) =>
        _chunks.Exists(dimensionType, x, z);

    public override ChunkColumn? LoadChunk(DimensionType dimensionType, int x, int z) =>
        _chunks.Load(dimensionType, x, z);

    public override void SaveChunk(ChunkColumn chunk)
    {
        WorldWriteBatch batch = new();
        _chunks.Save(batch, chunk);
        _database.Write(batch);
    }

    public override void DeleteChunk(DimensionType dimensionType, int x, int z)
    {
        WorldWriteBatch batch = new();
        _chunks.Delete(batch, dimensionType, x, z);
        _database.Write(batch);
    }

    public override void Dispose() => _database.Dispose();
}
