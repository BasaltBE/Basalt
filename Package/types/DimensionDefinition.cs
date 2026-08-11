using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DimensionDefinition {
    public int HeightMaximum;
    public int HeightMinimum;
    public GeneratorType GeneratorType;
    public DimensionType DimensionType = new();
    public UUID PackId = new();

    public void Read(BinaryReader reader) {
        HeightMaximum = reader.ReadZigZag();
        HeightMinimum = reader.ReadZigZag();
        GeneratorType = (global::BedrockProtocol.Enums.GeneratorType)reader.ReadZigZag();
        DimensionType.Read(reader);
        PackId.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(HeightMaximum);
        writer.WriteZigZag(HeightMinimum);
        writer.WriteZigZag((int)GeneratorType);
        DimensionType.Write(writer);
        PackId.Write(writer);
    }
}
