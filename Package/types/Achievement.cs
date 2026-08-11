using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Achievement : LegacyTelemetryEventEventDataVariant {
    public AchievementIds AchievementID;

    public void Read(BinaryReader reader) {
        AchievementID = (global::BedrockProtocol.Enums.AchievementIds)reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)AchievementID);
    }
}
