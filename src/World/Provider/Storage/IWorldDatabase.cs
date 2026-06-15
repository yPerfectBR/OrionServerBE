using System.Runtime.InteropServices;

namespace Orion.World.Provider.Storage;

public interface IWorldDatabase : IDisposable
{
    byte[]? Get(byte[] key);

    void Put(byte[] key, byte[] value);

    void Delete(byte[] key);

    void Write(WorldWriteBatch batch);

    IEnumerable<KeyValuePair<byte[], byte[]>> Iterate(byte[] prefix);
}

public sealed class WorldWriteBatch
{
    private readonly List<WriteOp> _ops = [];

    public void Put(byte[] key, byte[] value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        _ops.Add(new WriteOp(key, value));
    }

    public void Delete(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _ops.Add(new WriteOp(key, null));
    }

    internal ReadOnlySpan<WriteOp> Ops => CollectionsMarshal.AsSpan(_ops);

    internal readonly struct WriteOp(byte[] key, byte[]? value)
    {
        public byte[] Key { get; } = key;
        public byte[]? Value { get; } = value;
        public bool IsDelete => Value is null;
    }
}
