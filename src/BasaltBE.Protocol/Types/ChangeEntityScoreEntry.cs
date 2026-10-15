using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ChangeEntityScoreEntry : ScorePacketEntry {
    public override ScorePacketEntryAction Action => ScorePacketEntryAction.ChangeEntity;
    public long ScoreboardId;
    public string ObjectiveName = string.Empty;
    public int ScoreValue;
    public long ActorUniqueId;

    public override void Read(ref BinaryReader reader) {
        ScoreboardId = reader.ReadZigZong();
        ObjectiveName = reader.ReadVarString();
        ScoreValue = reader.ReadInt32(true);
        ActorUniqueId = reader.ReadZigZong();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZong(ScoreboardId);
        writer.WriteVarString(ObjectiveName);
        writer.WriteInt32(ScoreValue, true);
        writer.WriteZigZong(ActorUniqueId);
    }
}
