using Orion.Protocol.Enums;
using Orion.Player;
using Orion.RakNet;
using Orion.Scheduling.Messages;
using ChunkColumn = Orion.World.Chunk.Chunk;

namespace Orion.Scheduling;

public sealed class NoopAreaScheduler : IAreaScheduler
{
    public bool IsActive => false;

    public void Start()
    {
    }

    public void Stop()
    {
    }

    public void RequestAttachArea(Dimension dimension, int areaIndex)
    {
        _ = dimension;
        _ = areaIndex;
    }

    public void RequestDetachArea(Dimension dimension, int areaIndex)
    {
        _ = dimension;
        _ = areaIndex;
    }

    public void EnqueueAreaPacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> payload)
    {
        _ = connection;
        _ = packetId;
        _ = payload;
    }

    public void EnqueueAreaDisconnect(NetworkConnection connection)
    {
        _ = connection;
    }

    public void RunOnAreaThread(AreaHandle area, Action action) => action();

    public void BeginAreaTransfer(PlayerSession? session, AreaEntitySnapshot snapshot)
    {
        _ = session;
        _ = snapshot;
    }

    public IReadOnlyList<AreaWorkerLoadMetrics> GetMetrics() => [];

    public int? GetAttachedWorkerId(Dimension dimension, int areaIndex)
    {
        _ = dimension;
        _ = areaIndex;
        return null;
    }

    public void EnqueueCompletedChunk(Dimension dimension, int areaIndex, long hash, ChunkColumn? chunk)
    {
        dimension.CommitCompletedChunk(hash, chunk);
    }
}
