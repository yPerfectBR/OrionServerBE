using Orion.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Io;

public static class BatchCodec
{
    public static int EncodePackets(ReadOnlySpan<byte> input, Span<byte> output)
    {
        input.CopyTo(output);
        return input.Length;
    }

    public static int EncodePackets(ReadOnlyMemory<byte>[] packets, Span<byte> output)
    {
        int offset = 0;
        BinaryWriter writer = new(output, ref offset);

        foreach (ReadOnlyMemory<byte> packet in packets)
        {
            writer.WriteVarUInt((uint)packet.Length);
            writer.WriteBytes(packet.Span);
        }

        return offset;
    }

    public static ReadOnlyMemory<byte>[] DecodePackets(ReadOnlyMemory<byte> frame)
    {
        ReadOnlySpan<byte> span = frame.Span;
        int offset = 0;
        BinaryReader reader = new(span, ref offset);
        List<ReadOnlyMemory<byte>> packets = [];

        while (reader.Remaining > 0)
        {
            int packetLength = checked((int)reader.ReadVarUInt());
            if (packetLength <= 0 || packetLength > reader.Remaining)
            {
                break;
            }

            int packetOffset = reader.Offset;
            reader.Advance(packetLength);
            packets.Add(frame.Slice(packetOffset, packetLength));
        }

        return [..packets];
    }

    public static List<DataPacket> DecodePacketObjects(ReadOnlyMemory<byte> frame)
    {
        ReadOnlyMemory<byte>[] rawPackets = DecodePackets(frame);
        List<DataPacket> packets = new(rawPackets.Length);

        foreach (ReadOnlyMemory<byte> raw in rawPackets)
        {
            packets.Add(PacketCodec.DeserializeFromBytes(raw.Span));
        }

        return packets;
    }
}
