using Orion.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Packets;

/// <summary>
/// Server → client: show or hide individual HUD elements (/hud).
/// </summary>
[Packet(PacketId.SetHud)]
public sealed record SetHudPacket : DataPacket
{
    public List<HudElement> Elements = [];
    public HudVisibility Visibility;

    public override void Deserialize(BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        Elements = new List<HudElement>(count);
        for (int i = 0; i < count; i++)
        {
            Elements.Add((HudElement)reader.ReadZigZag());
        }

        Visibility = (HudVisibility)reader.ReadZigZag();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Elements.Count);
        for (int i = 0; i < Elements.Count; i++)
        {
            writer.WriteZigZag((int)Elements[i]);
        }

        writer.WriteZigZag((int)Visibility);
    }
}
