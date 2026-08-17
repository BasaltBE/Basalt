#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RemoveScore : SetScoreScoreInfoVariant {
    public ScorePacketEntryAction Action = global::BedrockProtocol.Enums.ScorePacketEntryAction.Remove;
    public ScoreboardId ScoreboardId = new();
    public string? ObjectiveName;

    public void Read(BinaryReader reader) {
        global::BedrockProtocol.Enums.ScorePacketEntryAction constValue0 = (global::BedrockProtocol.Enums.ScorePacketEntryAction)reader.ReadUInt8();
        if (constValue0 != global::BedrockProtocol.Enums.ScorePacketEntryAction.Remove) {
            throw new FormatException($"Expected remove for Action, got {constValue0}.");
        }
        ScoreboardId.Read(reader);
        if (reader.ReadBool()) {
            if (reader.ReadBool()) {
                ObjectiveName = reader.ReadVarString();
            } else {
                ObjectiveName = default;
            }
        } else {
            ObjectiveName = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)global::BedrockProtocol.Enums.ScorePacketEntryAction.Remove);
        ScoreboardId.Write(writer);
        writer.WriteBool(true);
        writer.WriteBool(ObjectiveName is not null);
        if (ObjectiveName is { } optionalValue5) {
            writer.WriteVarString(optionalValue5);
        }
    }
}
