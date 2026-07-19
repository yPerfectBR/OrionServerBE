namespace Orion.Network;

using Orion.Network.Handlers;
using Orion.Protocol.Enums;
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
            coordinator.EnqueueSend(session, packets);
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

    /// <summary>
    /// Sends gamemode change packets in order on the session thread.
    /// Matches Basalt: SetPlayerGameType + UpdateAbilities only (no ItemRegistry / CreativeContent).
    /// </summary>
    public static void SendGamemodeChange(
        PlayerSession session,
        string username,
        Gamemode gamemode,
        UpdateAbilitiesPacket abilitiesPacket)
    {
        void SendOrdered()
        {
            lock (session.Connection)
            {
                session.Network.SendPacket(session.Connection, new SetPlayerGameTypePacket { GameType = gamemode });
                session.Network.SendPacket(session.Connection, abilitiesPacket);
                CreativeInventoryLog.LogSetGamemodeSequence(username, gamemode);
            }
        }

        if (session.Network.Server.ConnectionCoordinator is ConnectionCoordinator coordinator && coordinator.IsActive)
        {
            coordinator.RunOnSessionThread(session, SendOrdered);
            return;
        }

        SendOrdered();
    }
}
