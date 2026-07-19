using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

/// <summary>
/// Requests execution of a server-side command.
/// </summary>
[Packet(PacketId.CommandRequest)]
public sealed record CommandRequestPacket : DataPacket
{
    /// <summary>
    /// Raw command requested by the client.
    /// </summary>
    public string Command = string.Empty;

    /// <summary>
    /// Origin data for the command request.
    /// </summary>
    public CommandOrigin Origin = new();

    /// <summary>
    /// Whether this command request is internal.
    /// </summary>
    public bool Internal;

    /// <summary>
    /// Command version requested by the client.
    /// </summary>
    public string Version = string.Empty;

    public override void Deserialize(BinaryReader reader)
    {
        Command = reader.ReadVarString();
        Origin = new CommandOrigin();
        Origin.Read(reader);
        Internal = reader.ReadBool();
        Version = reader.ReadVarString();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarString(Command);
        Origin.Write(writer);
        writer.WriteBool(Internal);
        writer.WriteVarString(Version);
    }
}
