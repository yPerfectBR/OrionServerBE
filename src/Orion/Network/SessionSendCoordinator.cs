namespace Orion.Network;

using Orion.Protocol.Packets;
using Orion.Player;
using Orion.Scheduling;

/// <summary>
/// Per-connection ordered send path for outbound game packets (Phase 6c / INVARIANT R5).
/// </summary>
public static class SessionSendCoordinator
{
    public static void Send(PlayerSession session, DataPacket packet)
    {
        if (packet is null)
        {
            return;
        }

        Send(session, [packet]);
    }

    public static void Send(PlayerSession session, IReadOnlyList<DataPacket> packets)
    {
        if (packets.Count == 0)
        {
            return;
        }

        if (session.Network.Server.ConnectionCoordinator is ConnectionCoordinator coordinator && coordinator.IsActive)
        {
            coordinator.RunOnSessionThread(session, () => SendDirect(session, packets));
            return;
        }

        SendDirect(session, packets);
    }

    internal static void SendDirect(PlayerSession session, DataPacket packet)
    {
        SendDirect(session, [packet]);
    }

    internal static void SendDirect(PlayerSession session, IReadOnlyList<DataPacket> packets)
    {
        if (packets.Count == 0)
        {
            return;
        }

        lock (session.Connection)
        {
            if (packets.Count == 1)
            {
                session.Network.SendPacket(session.Connection, packets[0]);
                return;
            }

            session.Network.SendPackets(session.Connection, packets);
        }
    }
}
