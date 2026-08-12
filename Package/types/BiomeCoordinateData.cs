#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeCoordinateData {
    public int MinValueType;
    public ushort MinValue;
    public int MaxValueType;
    public ushort MaxValue;
    public uint GridOffset;
    public uint GridStepSize;
    public RandomDistributionType Distribution;

    public void Read(BinaryReader reader) {
        MinValueType = reader.ReadZigZag();
        MinValue = reader.ReadUInt16(true);
        MaxValueType = reader.ReadZigZag();
        MaxValue = reader.ReadUInt16(true);
        GridOffset = reader.ReadUInt32(true);
        GridStepSize = reader.ReadUInt32(true);
        Distribution = (global::BedrockProtocol.Enums.RandomDistributionType)reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(MinValueType);
        writer.WriteUInt16(MinValue, true);
        writer.WriteZigZag(MaxValueType);
        writer.WriteUInt16(MaxValue, true);
        writer.WriteUInt32(GridOffset, true);
        writer.WriteUInt32(GridStepSize, true);
        writer.WriteZigZag((int)Distribution);
    }
}
