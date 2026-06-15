using Orion.Protocol.Enums;
using Orion.RakNet;

namespace Orion.Scheduling;

public interface INetworkScheduler
{
    void Start();

    void Stop();

    void EnqueueGamePacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> payload);

    void EnqueueDisconnect(NetworkConnection connection);

    void DrainMainQueue();

    IReadOnlyList<WorkerLoadMetrics> GetMetrics();
}
