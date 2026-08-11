using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SplineInstruction {
    public float TotalTime;
    public byte Type;
    public List<Vec3> Curve = [];
    public List<SplineProgressOption> ProgressKeyFrames = [];
    public List<SplineRotationOption> RotationOption = [];
    public string? SplineIdentifier;
    public bool? LoadFromJson;

    public void Read(BinaryReader reader) {
        TotalTime = reader.ReadF32(true);
        Type = reader.ReadUInt8();
        int count4 = checked((int)reader.ReadVarUInt());
        Curve = new List<Vec3>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            Vec3 item4 = default!;
            Vec3 readValue1004 = new();
            readValue1004.Read(reader);
            item4 = readValue1004;
            Curve.Add(item4);
        }
        int count6 = checked((int)reader.ReadVarUInt());
        ProgressKeyFrames = new List<SplineProgressOption>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            SplineProgressOption item6 = default!;
            SplineProgressOption readValue1006 = new();
            readValue1006.Read(reader);
            item6 = readValue1006;
            ProgressKeyFrames.Add(item6);
        }
        int count8 = checked((int)reader.ReadVarUInt());
        RotationOption = new List<SplineRotationOption>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            SplineRotationOption item8 = default!;
            SplineRotationOption readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            RotationOption.Add(item8);
        }
        if (reader.ReadBool()) {
            SplineIdentifier = reader.ReadVarString();
        } else {
            SplineIdentifier = default;
        }
        if (reader.ReadBool()) {
            LoadFromJson = reader.ReadBool();
        } else {
            LoadFromJson = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(TotalTime, true);
        writer.WriteUInt8(Type);
        writer.WriteVarUInt(checked((uint)Curve.Count));
        foreach (var item5 in Curve) {
            item5.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)ProgressKeyFrames.Count));
        foreach (var item7 in ProgressKeyFrames) {
            item7.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)RotationOption.Count));
        foreach (var item9 in RotationOption) {
            item9.Write(writer);
        }
        writer.WriteBool(SplineIdentifier is not null);
        if (SplineIdentifier is { } optionalValue11) {
            writer.WriteVarString(optionalValue11);
        }
        writer.WriteBool(LoadFromJson is not null);
        if (LoadFromJson is { } optionalValue13) {
            writer.WriteBool(optionalValue13);
        }
    }
}
