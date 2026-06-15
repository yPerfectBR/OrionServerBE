using Orion.Config;
using ChunkColumn = Orion.World.Chunk.Chunk;
using System.Linq;

namespace Orion.World.Threading;

/// <summary>
/// Maps area indices to <see cref="AreaShard"/> instances for a dimension.
/// </summary>
public sealed class AreaShardManager
{
    private readonly AreaShard[] _shards;
    private readonly AreaResolver _resolver;

    public AreaShardManager(IReadOnlyList<ThreadingAreaConfig> areas)
    {
        _resolver = new AreaResolver(areas);
        _shards = new AreaShard[areas.Count + 1];
        _shards[AreaResolver.DefaultThread] = AreaShard.CreateDefault();

        for (int i = 0; i < areas.Count; i++)
        {
            ThreadingAreaConfig area = areas[i];
            _shards[i + 1] = new AreaShard(
                i + 1,
                area.Name,
                area.Start[0],
                area.Start[1],
                area.End[0],
                area.End[1]);
        }
    }

    public AreaResolver Resolver => _resolver;
    public int ShardCount => _shards.Length;
    public IEnumerable<AreaShard> Shards => _shards;

    public AreaShard GetDefaultShard() => _shards[AreaResolver.DefaultThread];

    public AreaShard GetShard(int areaIndex)
    {
        if ((uint)areaIndex >= (uint)_shards.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(areaIndex));
        }

        return _shards[areaIndex];
    }

    public AreaShard ResolveShard(int chunkX, int chunkZ) => GetShard(_resolver.ResolveArea(chunkX, chunkZ));

    public int TotalChunkCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _shards.Length; i++)
            {
                count += _shards[i].ChunkCount;
            }

            return count;
        }
    }

    public IEnumerable<ChunkColumn> AllChunks
    {
        get
        {
            for (int i = 0; i < _shards.Length; i++)
            {
                foreach (ChunkColumn chunk in _shards[i].Chunks.ToArray())
                {
                    yield return chunk;
                }
            }
        }
    }
}
