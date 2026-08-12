#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SpawnSettings {
    public SpawnBiomeType SpawnBiomeType;
    public string UserDefinedBiomeName = string.Empty;
    public int Dimension;

    public void Read(BinaryReader reader) {
        SpawnBiomeType = (global::BedrockProtocol.Enums.SpawnBiomeType)reader.ReadInt16(true);
        UserDefinedBiomeName = reader.ReadVarString();
        Dimension = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt16((short)SpawnBiomeType, true);
        writer.WriteVarString(UserDefinedBiomeName);
        writer.WriteZigZag(Dimension);
    }
}
