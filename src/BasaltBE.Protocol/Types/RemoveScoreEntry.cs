using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class RemoveScoreEntry : ScorePacketEntry {
    public override ScorePacketEntryAction Action => ScorePacketEntryAction.Remove;
    public long ScoreboardId;
    public string? ObjectiveName;

    public override void Read(ref BinaryReader reader) {
        ScoreboardId = reader.ReadZigZong();
        ObjectiveName = reader.ReadBool() ? reader.ReadVarString() : null;
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZong(ScoreboardId);
        writer.WriteBool(ObjectiveName is not null);
        if (ObjectiveName is string objectiveName)
            writer.WriteVarString(objectiveName);
    }
}
