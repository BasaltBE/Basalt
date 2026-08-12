#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeScatterParamData {
    public List<BiomeCoordinateData> Coordinates = [];
    public CoordinateEvaluationOrder EvalOrder;
    public int ChancePercentType;
    public ushort ChancePercent;
    public int ChanceNumerator;
    public int ChanceDenominator;
    public int IterationsType;
    public ushort Iterations;

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Coordinates = new List<BiomeCoordinateData>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            BiomeCoordinateData item0 = default!;
            BiomeCoordinateData readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Coordinates.Add(item0);
        }
        EvalOrder = (global::BedrockProtocol.Enums.CoordinateEvaluationOrder)reader.ReadZigZag();
        ChancePercentType = reader.ReadZigZag();
        ChancePercent = reader.ReadUInt16(true);
        ChanceNumerator = reader.ReadInt32(true);
        ChanceDenominator = reader.ReadInt32(true);
        IterationsType = reader.ReadZigZag();
        Iterations = reader.ReadUInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Coordinates.Count));
        foreach (var item1 in Coordinates) {
            item1.Write(writer);
        }
        writer.WriteZigZag((int)EvalOrder);
        writer.WriteZigZag(ChancePercentType);
        writer.WriteUInt16(ChancePercent, true);
        writer.WriteInt32(ChanceNumerator, true);
        writer.WriteInt32(ChanceDenominator, true);
        writer.WriteZigZag(IterationsType);
        writer.WriteUInt16(Iterations, true);
    }
}
