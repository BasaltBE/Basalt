using Basalt.BedrockProtocol.Enums;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ScoreEntry : DataType {
    public ScorePacketEntryAction Action;
    public ScoreboardId ScoreboardId = new();
    public string ObjectiveName = string.Empty;
    public int ScoreValue;
    public long PlayerUniqueId;
    public long ActorId;
    public string FakePlayerName = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Action);
        writer.WriteVarString(Action switch {
            ScorePacketEntryAction.Remove => "remove",
            ScorePacketEntryAction.ChangePlayer => "changeplayer",
            ScorePacketEntryAction.ChangeEntity => "changeentity",
            ScorePacketEntryAction.ChangeFakePlayer => "changefakeplayer",
            _ => throw new InvalidOperationException($"Unsupported score entry action: {Action}")
        });
        ScoreboardId.Write(ref writer);
        if (Action == ScorePacketEntryAction.Remove) {
            writer.WriteBool(ObjectiveName.Length != 0);
            if (ObjectiveName.Length != 0) writer.WriteVarString(ObjectiveName);
            return;
        }

        writer.WriteVarString(ObjectiveName);
        writer.WriteInt32(ScoreValue, true);
        switch (Action) {
            case ScorePacketEntryAction.ChangePlayer: writer.WriteVarLong(PlayerUniqueId); break;
            case ScorePacketEntryAction.ChangeEntity: writer.WriteVarLong(ActorId); break;
            case ScorePacketEntryAction.ChangeFakePlayer: writer.WriteVarString(FakePlayerName); break;
        }
    }

    public override void Read(ref BinaryReader reader) {
        Action = (ScorePacketEntryAction)reader.ReadVarUInt();
        _ = reader.ReadVarString();
        ScoreboardId.Read(ref reader);
        if (Action == ScorePacketEntryAction.Remove) {
            ObjectiveName = reader.ReadBool() ? reader.ReadVarString() : string.Empty;
            return;
        }

        ObjectiveName = reader.ReadVarString();
        ScoreValue = reader.ReadInt32(true);
        switch (Action) {
            case ScorePacketEntryAction.ChangePlayer: PlayerUniqueId = reader.ReadVarLong(); break;
            case ScorePacketEntryAction.ChangeEntity: ActorId = reader.ReadVarLong(); break;
            case ScorePacketEntryAction.ChangeFakePlayer: FakePlayerName = reader.ReadVarString(); break;
        }
    }
}
