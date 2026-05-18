using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record LevelSoundEventPacket : DataPacket
{
    public LevelSoundEvent Event { get; set; }
    public Vec3f Position { get; set; }
    public int Data { get; set; }
    public string ActorIdentifier { get; set; } = string.Empty;
    public bool IsBabyMob { get; set; }
    public bool IsGlobal { get; set; }
    public long UniqueActorId { get; set; }
    public Optional<Vec3f> FireAtPosition { get; set; } = new();

    public override PacketId PacketId => PacketId.LevelSoundEvent;

    public override void Deserialize(BinaryReader reader)
    {
        Event = (LevelSoundEvent)reader.ReadVarUInt();
        Vec3f position = Position;
        position.Read(reader);
        Position = position;
        Data = reader.ReadVarInt(); // ZigZag
        ActorIdentifier = reader.ReadVarString();
        IsBabyMob = reader.ReadBool();
        IsGlobal = reader.ReadBool();
        UniqueActorId = reader.ReadInt64(true);
        FireAtPosition.Read(reader);
    }

    public override void Serialize(BinaryWriter writer)
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
