#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ChangePlayerScore : SetScoreScoreInfoVariant {
    public ScorePacketEntryAction Action = global::BedrockProtocol.Enums.ScorePacketEntryAction.ChangePlayer;
    public ScoreboardId ScoreboardId = new();
    public string ObjectiveName = string.Empty;
    public int ScoreValue;
    public PlayerScoreboardId PlayerUniqueId = new();

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ScorePacketEntryAction constValue0 = (global::BedrockProtocol.Enums.ScorePacketEntryAction)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ScorePacketEntryAction.ChangePlayer) {
            throw new FormatException($"Expected changeplayer for Action, got {constValue0}.");
        }
        ScoreboardId.Read(reader);
        ObjectiveName = reader.ReadVarString();
        ScoreValue = reader.ReadInt32(true);
        PlayerUniqueId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ScorePacketEntryAction.ChangePlayer);
        ScoreboardId.Write(writer);
        writer.WriteVarString(ObjectiveName);
        writer.WriteInt32(ScoreValue, true);
        PlayerUniqueId.Write(writer);
    }
}
