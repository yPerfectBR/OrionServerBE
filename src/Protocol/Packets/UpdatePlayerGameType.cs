using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;

namespace Orion.Protocol.Packets;

[Packet(PacketId.UpdatePlayerGameType)]
public sealed record UpdatePlayerGameTypePacket : DataPacket
{
    public Gamemode GameType;
    public long PlayerUniqueId;
    public ulong Tick;

    public override void Deserialize(BinaryReader reader)
    {
        GameType = (Gamemode)reader.ReadZigZag();
        PlayerUniqueId = reader.ReadZigZong();
        Tick = reader.ReadVarULong();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteZigZag((int)GameType);
        writer.WriteZigZong(PlayerUniqueId);
        writer.WriteVarULong(Tick);
    }
}
