using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Orion.Protocol.Enums;
using Orion.Protocol.Packets;
using Orion.Protocol.Types;

namespace Orion.Protocol.Packets;

[Packet(PacketId.LevelSoundEvent)]
public sealed record LevelSoundEventPacket : DataPacket
{
    /// <summary>
    /// Level sound event identifier (protocol 1001+).
    /// </summary>
    public string SoundEvent = string.Empty;

    /// <summary>
    /// Legacy enum accessor for game code. Serializes via <see cref="SoundEvent"/>.
    /// </summary>
    public LevelSoundEvent Event
    {
        get => LevelSoundEventIdentifiers.TryParse(SoundEvent, out LevelSoundEvent value)
            ? value
            : LevelSoundEvent.Undefined;
        set => SoundEvent = LevelSoundEventIdentifiers.ToIdentifier(value);
    }

    /// <summary>
    /// Sound world position.
    /// </summary>
    public Vec3f Position;

    /// <summary>
    /// Event-specific data value.
    /// </summary>
    public int Data;

    /// <summary>
    /// Actor identifier text.
    /// </summary>
    public string ActorIdentifier = string.Empty;

    /// <summary>
    /// Whether actor is a baby variant.
    /// </summary>
    public bool IsBabyMob;

    /// <summary>
    /// Whether sound should be global.
    /// </summary>
    public bool IsGlobal;

    /// <summary>
    /// Unique actor id tied to this sound.
    /// </summary>
    public long UniqueActorId;

    /// <summary>
    /// Optional fire-at position payload.
    /// </summary>
    public Optional<Vec3f> FireAtPosition = new();

    public override void Deserialize(BinaryReader reader)
    {
        SoundEvent = reader.ReadVarString();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Data = reader.ReadZigZag();
        ActorIdentifier = reader.ReadVarString();
        IsBabyMob = reader.ReadBool();
        IsGlobal = reader.ReadBool();
        UniqueActorId = reader.ReadInt64(true);
        FireAtPosition.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarString(SoundEvent);
        Position.Write(writer);
        writer.WriteZigZag(Data);
        writer.WriteVarString(ActorIdentifier);
        writer.WriteBool(IsBabyMob);
        writer.WriteBool(IsGlobal);
        writer.WriteInt64(UniqueActorId, true);
        FireAtPosition.Write(writer);
    }
}
