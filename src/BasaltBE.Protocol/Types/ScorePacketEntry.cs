using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public abstract class ScorePacketEntry : DataType {
    public abstract ScorePacketEntryAction Action { get; }

    public static ScorePacketEntry ReadEntry(ref BinaryReader reader) {
        ScorePacketEntryAction action = Enum.Parse<ScorePacketEntryAction>(reader.ReadVarString(), true);
        ScorePacketEntry entry = action switch {
            ScorePacketEntryAction.Remove => new RemoveScoreEntry(),
            ScorePacketEntryAction.ChangePlayer => new ChangePlayerScoreEntry(),
            ScorePacketEntryAction.ChangeEntity => new ChangeEntityScoreEntry(),
            ScorePacketEntryAction.ChangeFakePlayer => new ChangeFakePlayerScoreEntry(),
            _ => throw new InvalidDataException($"Unknown score packet entry action {(uint)action}.")
        };
        entry.Read(ref reader);
        return entry;
    }

    public void WriteEntry(ref BinaryWriter writer) {
        writer.WriteVarString(Action.ToString());
        Write(ref writer);
    }
}
