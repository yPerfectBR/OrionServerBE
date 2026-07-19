using Orion.Config;
using Log = Orion.Logger.Logger;
using Orion.Protocol.Enums;
using Orion.RakNet;

namespace Orion.Scheduling;

public sealed class PacketIngress
{
    private readonly Server _server;

    public PacketIngress(Server server) => _server = server;

    public void Route(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> payload)
    {
        if (IsGlobalPacket(packetId))
        {
            if (_server.Properties.WorldSchedulerDebug)
            {
                Log.Debug(LogCategory.Orion, "[PacketIngress] inline packet={0}", packetId);
            }

            _server.Network.HandleGamePacketOnWorker(connection, packetId, payload);
            return;
        }

        if (!_server.Sessions.ContainsKey(connection))
        {
            return;
        }

        if (_server.ConnectionCoordinator is ConnectionCoordinator coordinator && coordinator.IsActive)
        {
            coordinator.EnqueueSessionPacket(connection, packetId, payload);
            return;
        }

        if (_server.Properties.AreaThreadingEnabled && _server.AreaScheduler.IsActive)
        {
            _server.AreaScheduler.EnqueueAreaPacket(connection, packetId, payload);
            return;
        }

        _server.Scheduler.EnqueueGamePacket(connection, packetId, payload);
    }

    public static bool IsGlobalPacket(PacketId packetId) =>
        packetId is PacketId.Login or PacketId.RequestNetworkSettings;
}
