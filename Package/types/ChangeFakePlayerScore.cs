using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ChangeFakePlayerScore : SetScoreScoreInfoVariant {
    public ScorePacketEntryAction Action = global::BedrockProtocol.Enums.ScorePacketEntryAction.ChangeFakePlayer;
    public ScoreboardId ScoreboardId = new();
    public string ObjectiveName = string.Empty;
    public int ScoreValue;
    public string FakePlayerName = string.Empty;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ScorePacketEntryAction constValue0 = (global::BedrockProtocol.Enums.ScorePacketEntryAction)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ScorePacketEntryAction.ChangeFakePlayer) {
            throw new FormatException($"Expected changefakeplayer for Action, got {constValue0}.");
        }
        ScoreboardId.Read(reader);
        ObjectiveName = reader.ReadVarString();
        ScoreValue = reader.ReadInt32(true);
        FakePlayerName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ScorePacketEntryAction.ChangeFakePlayer);
        ScoreboardId.Write(writer);
        writer.WriteVarString(ObjectiveName);
        writer.WriteInt32(ScoreValue, true);
        writer.WriteVarString(FakePlayerName);
    }
}
