namespace Orion.PluginContracts.Network;

public interface IPacketPipeline
{
    /// <summary>Subscribe to inbound packets after framing/decode of PacketId, before core handler switch.</summary>
    void OnReceive(PacketReceiveHook hook);

    /// <summary>Subscribe to outbound packets before write to connection.</summary>
    void OnSend(PacketSendHook hook);

    /// <summary>Claim exclusive handling for a PacketId. Second owner is rejected.</summary>
    bool TryOwnHandler(int packetId, IOrionPlugin owner, PacketHandlerDelegate handler);
}
