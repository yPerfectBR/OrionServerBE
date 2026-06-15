using Orion.Protocol.Nbt;
using Orion.Protocol.Types;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.World.Block;

[Tag(TagType.Compound)]
public sealed class BlockLevelStorage : CompoundTag
{
    private readonly ChunkColumn? _chunk;

    public BlockLevelStorage(ChunkColumn? chunk = null, CompoundTag? source = null)
    {
        _chunk = chunk;
        if (source is null)
        {
            return;
        }

        foreach ((string key, BaseTag value) in source.Values)
        {
            Values[key] = value;
        }
    }

    public new BlockLevelStorage Set(string key, BaseTag value)
    {
        base.Set(key, value);
        MarkDirtyIfNeeded();
        return this;
    }

    public BlockPos GetPosition()
    {
        int x = Get<IntTag>("x")?.Value ?? 0;
        int y = Get<IntTag>("y")?.Value ?? 0;
        int z = Get<IntTag>("z")?.Value ?? 0;
        return new BlockPos { X = x, Y = y, Z = z };
    }

    public void SetPosition(BlockPos position)
    {
        Set("x", new IntTag { Name = "x", Value = position.X });
        Set("y", new IntTag { Name = "y", Value = position.Y });
        Set("z", new IntTag { Name = "z", Value = position.Z });
    }

    private void MarkDirtyIfNeeded()
    {
        if (_chunk is not null)
        {
            _chunk.Dirty = true;
        }
    }
}
