using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketConstrainedValueData {
    public uint EnumValueSymbol;
    public uint EnumSymbol;
    public List<byte> ConstraintIndices = [];

    public void Read(BinaryReader reader) {
        EnumValueSymbol = reader.ReadUInt32(true);
        EnumSymbol = reader.ReadUInt32(true);
        int count4 = checked((int)reader.ReadVarUInt());
        ConstraintIndices = new List<byte>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            byte item4 = default!;
            item4 = reader.ReadUInt8();
            ConstraintIndices.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(EnumValueSymbol, true);
        writer.WriteUInt32(EnumSymbol, true);
        writer.WriteVarUInt(checked((uint)ConstraintIndices.Count));
        foreach (var item5 in ConstraintIndices) {
            writer.WriteUInt8(item5);
        }
    }
}
