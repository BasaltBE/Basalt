using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializableVoxelShape {
    public SerializableCells Cells = new();
    public List<float> XCoordinates = [];
    public List<float> YCoordinates = [];
    public List<float> ZCoordinates = [];

    public void Read(BinaryReader reader) {
        Cells.Read(reader);
        int count2 = checked((int)reader.ReadVarUInt());
        XCoordinates = new List<float>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            float item2 = default!;
            item2 = reader.ReadF32(true);
            XCoordinates.Add(item2);
        }
        int count4 = checked((int)reader.ReadVarUInt());
        YCoordinates = new List<float>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            float item4 = default!;
            item4 = reader.ReadF32(true);
            YCoordinates.Add(item4);
        }
        int count6 = checked((int)reader.ReadVarUInt());
        ZCoordinates = new List<float>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            float item6 = default!;
            item6 = reader.ReadF32(true);
            ZCoordinates.Add(item6);
        }
    }

    public void Write(BinaryWriter writer) {
        Cells.Write(writer);
        writer.WriteVarUInt(checked((uint)XCoordinates.Count));
        foreach (var item3 in XCoordinates) {
            writer.WriteF32(item3, true);
        }
        writer.WriteVarUInt(checked((uint)YCoordinates.Count));
        foreach (var item5 in YCoordinates) {
            writer.WriteF32(item5, true);
        }
        writer.WriteVarUInt(checked((uint)ZCoordinates.Count));
        foreach (var item7 in ZCoordinates) {
            writer.WriteF32(item7, true);
        }
    }
}
