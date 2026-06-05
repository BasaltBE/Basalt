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
    public LevelSoundEvent Event;

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

    public override void Deserialize(Binary.BinaryReader reader)
    {
        Event = (LevelSoundEvent)reader.ReadVarUInt();

        Vec3f position = Position;
        position.Read(reader);
        Position = position;

        Data = reader.ReadVarInt();
        ActorIdentifier = reader.ReadVarString();
        IsBabyMob = reader.ReadBool();
        IsGlobal = reader.ReadBool();
        UniqueActorId = reader.ReadInt64(true);
        FireAtPosition.Read(reader);
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarUInt((uint)Event);
        Position.Write(writer);
        writer.WriteZigZag(Data);
        writer.WriteVarString(ActorIdentifier);
        writer.WriteBool(IsBabyMob);
        writer.WriteBool(IsGlobal);
        writer.WriteInt64(UniqueActorId, true);
        FireAtPosition.Write(writer);
    }
}
