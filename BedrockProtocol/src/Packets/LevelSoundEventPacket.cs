using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(123)]
public sealed class LevelSoundEventPacket : DataPacket {
    public string SoundEvent = string.Empty;
    public Vec3 Position = new();
    public int Data;
    public string ActorIdentifier = string.Empty;
    public bool IsBaby;
    public bool IsGlobal;
    public long ActorUniqueId;
    public Vec3? FireAtPosition;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(SoundEvent);
        Position.Write(ref writer);
        writer.WriteVarInt(Data);
        writer.WriteVarString(ActorIdentifier);
        writer.WriteBool(IsBaby);
        writer.WriteBool(IsGlobal);
        writer.WriteInt64(ActorUniqueId, true);
        writer.WriteBool(FireAtPosition is not null);
        if (FireAtPosition is Vec3 fireAtPosition) fireAtPosition.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        SoundEvent = reader.ReadVarString();
        Position.Read(ref reader);
        Data = reader.ReadVarInt();
        ActorIdentifier = reader.ReadVarString();
        IsBaby = reader.ReadBool();
        IsGlobal = reader.ReadBool();
        ActorUniqueId = reader.ReadInt64(true);
        FireAtPosition = reader.ReadBool() ? new Vec3() : null;
        FireAtPosition?.Read(ref reader);
    }
}
