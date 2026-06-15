using LevelDB;

namespace Orion.World.Provider.Storage;

public sealed class LevelDbDatabase : IWorldDatabase
{
    private readonly DB _database;

    public LevelDbDatabase(string path)
    {
        Options options = new() { CreateIfMissing = true };
        _database = new DB(options, path);
    }

    public byte[]? Get(byte[] key)
    {
        byte[]? data = _database.Get(key);
        return data is { Length: > 0 } ? data : null;
    }

    public void Put(byte[] key, byte[] value) => _database.Put(key, value);

    public void Delete(byte[] key) => _database.Delete(key);

    public void Write(WorldWriteBatch batch)
    {
        using WriteBatch writeBatch = new();
        ReadOnlySpan<WorldWriteBatch.WriteOp> ops = batch.Ops;

        for (int i = 0; i < ops.Length; i++)
        {
            WorldWriteBatch.WriteOp op = ops[i];
            if (op.IsDelete)
            {
                writeBatch.Delete(op.Key);
            }
            else
            {
                writeBatch.Put(op.Key, op.Value!);
            }
        }

        _database.Write(writeBatch);
    }

    public IEnumerable<KeyValuePair<byte[], byte[]>> Iterate(byte[] prefix)
    {
        using Iterator iterator = _database.CreateIterator(new ReadOptions());
        iterator.Seek(prefix);

        while (iterator.IsValid())
        {
            ReadOnlySpan<byte> key = iterator.Key();
            if (key.Length < prefix.Length || !key[..prefix.Length].SequenceEqual(prefix))
            {
                break;
            }

            byte[] value = iterator.Value().ToArray();
            if (value.Length > 0)
            {
                yield return new KeyValuePair<byte[], byte[]>(key.ToArray(), value);
            }

            iterator.Next();
        }
    }

    public void Dispose() => _database.Dispose();
}
