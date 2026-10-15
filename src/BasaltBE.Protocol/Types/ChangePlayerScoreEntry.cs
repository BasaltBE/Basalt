using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ChangePlayerScoreEntry : ScorePacketEntry {
    public override ScorePacketEntryAction Action => ScorePacketEntryAction.ChangePlayer;
    public long ScoreboardId;
    public string ObjectiveName = string.Empty;
    public int ScoreValue;
    public long PlayerUniqueId;

    public override void Read(ref BinaryReader reader) {
        ScoreboardId = reader.ReadZigZong();
        ObjectiveName = reader.ReadVarString();
        ScoreValue = reader.ReadInt32(true);
        PlayerUniqueId = reader.ReadZigZong();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZong(ScoreboardId);
        writer.WriteVarString(ObjectiveName);
        writer.WriteInt32(ScoreValue, true);
        writer.WriteZigZong(PlayerUniqueId);
    }
}
