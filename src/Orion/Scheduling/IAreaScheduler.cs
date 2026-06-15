using Orion.Protocol.Enums;
using Orion.Player;
using Orion.RakNet;
using Orion.Scheduling.Messages;
using Orion.World.Threading;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.Scheduling;

public readonly record struct AreaHandle(Dimension Dimension, int AreaIndex)
{
    public AreaShard Area => Dimension.GetAreaShard(AreaIndex);
}

public interface IAreaScheduler
{
    bool IsActive { get; }

    void Start();

    void Stop();

    void RequestAttachArea(Dimension dimension, int areaIndex);

    void RequestDetachArea(Dimension dimension, int areaIndex);

    void EnqueueAreaPacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> payload);

    void EnqueueAreaDisconnect(NetworkConnection connection);

    void RunOnAreaThread(AreaHandle area, Action action);

    void BeginAreaTransfer(PlayerSession? session, AreaEntitySnapshot snapshot);

    IReadOnlyList<AreaWorkerLoadMetrics> GetMetrics();

    int? GetAttachedWorkerId(Dimension dimension, int areaIndex);

    void EnqueueCompletedChunk(Dimension dimension, int areaIndex, long hash, ChunkColumn? chunk);
}
