using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ChangeFakePlayerScoreEntry : ScorePacketEntry {
    public override ScorePacketEntryAction Action => ScorePacketEntryAction.ChangeFakePlayer;
    public long ScoreboardId;
    public string ObjectiveName = string.Empty;
    public int ScoreValue;
    public string FakePlayerName = string.Empty;

    public override void Read(ref BinaryReader reader) {
        ScoreboardId = reader.ReadZigZong();
        ObjectiveName = reader.ReadVarString();
        ScoreValue = reader.ReadInt32(true);
        FakePlayerName = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZong(ScoreboardId);
        writer.WriteVarString(ObjectiveName);
        writer.WriteInt32(ScoreValue, true);
        writer.WriteVarString(FakePlayerName);
    }
}
