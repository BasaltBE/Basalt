using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.LevelSoundEvent)]
public sealed record LevelSoundEventPacket : DataPacket
{
    /// <summary>
    /// Level sound event id.
    /// </summary>
    public string Event = LevelSoundEvent.Undefined;

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
    public bool BabyMob;

    /// <summary>
    /// Whether distance-based volume is disabled.
    /// </summary>
    public bool DisableRelativeVolume;

    /// <summary>
    /// Unique actor id tied to this sound.
    /// </summary>
    public long UniqueActorId;

    /// <summary>
    /// Optional fire-at position payload.
    /// </summary>
    public Optional<Vec3f> FireAtPosition = new();

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Event = reader.ReadVarString();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Data = reader.ReadVarInt();
        ActorIdentifier = reader.ReadVarString();
        BabyMob = reader.ReadBool();
        DisableRelativeVolume = reader.ReadBool();
        UniqueActorId = reader.ReadInt64(true);
        FireAtPosition.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarString(Event);
        Position.Write(writer);
        writer.WriteZigZag(Data);
        writer.WriteVarString(ActorIdentifier);
        writer.WriteBool(BabyMob);
        writer.WriteBool(DisableRelativeVolume);
        writer.WriteInt64(UniqueActorId, true);
        FireAtPosition.Write(writer);
    }
}
