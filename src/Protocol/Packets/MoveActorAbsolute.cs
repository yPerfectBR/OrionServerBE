using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

[Packet(PacketId.MoveActorAbsolute)]
public sealed record MoveActorAbsolutePacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong EntityRuntimeId;

    /// <summary>
    /// Movement flags.
    /// </summary>
    public byte Flags;

    /// <summary>
    /// Absolute actor position.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Absolute actor rotation.
    /// Pitch -> X
    /// Yaw -> Y
    /// HeadYaw -> Z
    /// </summary>
    public Vec3f Rotation;

    public override void Deserialize(BinaryReader reader)
    {
        EntityRuntimeId = reader.ReadVarULong();
        Flags = reader.ReadUInt8();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Vec3f rotation = Rotation;
        rotation.X = ReadByteAngle(reader);
        rotation.Y = ReadByteAngle(reader);
        rotation.Z = ReadByteAngle(reader);
        Rotation = rotation;
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarULong(EntityRuntimeId);
        writer.WriteUInt8(Flags);
        Position.Write(writer);
        WriteByteAngle(writer, Rotation.X);
        WriteByteAngle(writer, Rotation.Y);
        WriteByteAngle(writer, Rotation.Z);
    }

    private static float ReadByteAngle(BinaryReader reader)
    {
        return reader.ReadUInt8() * (360f / 256f);
    }

    private static void WriteByteAngle(BinaryWriter writer, float value)
    {
        writer.WriteUInt8((byte)(value * (256f / 360f)));
    }
}
