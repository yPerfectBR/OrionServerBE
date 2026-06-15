using Orion.World;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.Scheduling.Messages;

public sealed class CompletedChunkMessage : IAreaMessage
{
    public required Dimension Dimension { get; init; }

    public required int AreaIndex { get; init; }

    public required long Hash { get; init; }

    public required ChunkColumn? Chunk { get; init; }
}
