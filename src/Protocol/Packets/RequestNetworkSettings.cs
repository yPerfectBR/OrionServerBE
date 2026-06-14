using Orion.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Packets;

namespace Orion.Protocol.Packets;

[Packet(PacketId.RequestNetworkSettings)]
public sealed record RequestNetworkSettingsPacket : DataPacket
{   
    /// <summary>
    /// Protocol version.
    /// This is used to determine if client and server are compatible. 
    /// If the protocol versions mismatch, then they are on different mc versions.
    /// </summary>
    public int Protocol;

    public RequestNetworkSettingsPacket() : this(Io.Constants.ProtocolVersion)
    {
    }

    public RequestNetworkSettingsPacket(int protocol)
    {
        Protocol = protocol;
    }

    public override void Deserialize(BinaryReader reader)
    {
        Protocol = reader.ReadInt32(false);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteInt32(Protocol, false);
    }
};
