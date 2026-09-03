using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class MovePlayerTeleportData : DataType {
    public int TeleportationCause;
    public int SourceActorType;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt32(TeleportationCause, true);
        writer.WriteInt32(SourceActorType, true);
    }

    public override void Read(ref BinaryReader reader) {
        TeleportationCause = reader.ReadInt32(true);
        SourceActorType = reader.ReadInt32(true);
    }
}
