using System.Reflection;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Orion.Protocol.Io;

public static class PacketCodec
{
    static readonly IReadOnlyDictionary<PacketId, Func<DataPacket>> Pool;
    static readonly IReadOnlyDictionary<Type, PacketId> TypeIds;

    static PacketCodec()
    {
        Dictionary<PacketId, Func<DataPacket>> pool = [];
        Dictionary<Type, PacketId> typeIds = [];

        foreach (Type type in typeof(DataPacket).Assembly.GetTypes())
        {
            if (!typeof(DataPacket).IsAssignableFrom(type) || type.IsAbstract)
            {
                continue;
            }

            PacketAttribute? attribute = type.GetCustomAttribute<PacketAttribute>();
            if (attribute is null)
            {
                continue;
            }

            if (pool.ContainsKey(attribute.Id))
            {
                throw new InvalidOperationException($"Duplicate packet id mapping for {attribute.Id}.");
            }

            pool[attribute.Id] = () =>
            {
                object? instance = Activator.CreateInstance(type);
                if (instance is not DataPacket packet)
                {
                    throw new InvalidOperationException($"{type.FullName} could not be created.");
                }

                return packet;
            };
            typeIds[type] = attribute.Id;
        }

        Pool = pool;
        TypeIds = typeIds;
    }

    public static DataPacket Deserialize(BinaryReader reader)
    {
        PacketId id = (PacketId)reader.ReadVarUInt();

        if (!Pool.TryGetValue(id, out Func<DataPacket>? create))
        {
            throw new NotImplementedException($"Deserialization for packet ID {(int)id} ({id}) is not implemented.");
        }

        DataPacket packet = create();
        packet.Deserialize(reader);
        return packet;
    }

    public static PacketId GetId(DataPacket packet)
    {
        Type type = packet.GetType();
        if (!TypeIds.TryGetValue(type, out PacketId id))
        {
            throw new NotImplementedException($"Packet id for {type.FullName} is not implemented.");
        }

        return id;
    }

    public static void Serialize(DataPacket packet, BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)GetId(packet));
        packet.Serialize(writer);
    }

    public static byte[] SerializeToBytes(DataPacket packet)
    {
        byte[] buffer = new byte[65536];
        int offset = 0;
        BinaryWriter writer = new(buffer, ref offset);
        Serialize(packet, writer);
        return buffer[..offset];
    }

    public static DataPacket DeserializeFromBytes(ReadOnlySpan<byte> packetBytes)
    {
        int offset = 0;
        BinaryReader reader = new(packetBytes, ref offset);
        return Deserialize(reader);
    }
}
