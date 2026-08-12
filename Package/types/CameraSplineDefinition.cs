#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraSplineDefinition {
    public string Name = string.Empty;
    public float TotalTime;
    public string? SplineType;
    public List<CameraSplineControlPoint> ControlPoints = [];
    public List<CameraSplineProgressKeyFrame> ProgressKeyFrames = [];
    public List<CameraSplineRotationKeyFrame> RotationKeyFrames = [];

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        TotalTime = reader.ReadF32(true);
        if (reader.ReadBool()) {
            SplineType = reader.ReadVarString();
        } else {
            SplineType = default;
        }
        int count6 = checked((int)reader.ReadVarUInt());
        ControlPoints = new List<CameraSplineControlPoint>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            CameraSplineControlPoint item6 = default!;
            CameraSplineControlPoint readValue1006 = new();
            readValue1006.Read(reader);
            item6 = readValue1006;
            ControlPoints.Add(item6);
        }
        int count8 = checked((int)reader.ReadVarUInt());
        ProgressKeyFrames = new List<CameraSplineProgressKeyFrame>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            CameraSplineProgressKeyFrame item8 = default!;
            CameraSplineProgressKeyFrame readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            ProgressKeyFrames.Add(item8);
        }
        int count10 = checked((int)reader.ReadVarUInt());
        RotationKeyFrames = new List<CameraSplineRotationKeyFrame>(count10);
        for (int i10 = 0; i10 < count10; i10++) {
            CameraSplineRotationKeyFrame item10 = default!;
            CameraSplineRotationKeyFrame readValue1010 = new();
            readValue1010.Read(reader);
            item10 = readValue1010;
            RotationKeyFrames.Add(item10);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteF32(TotalTime, true);
        writer.WriteBool(SplineType is not null);
        if (SplineType is { } optionalValue5) {
            writer.WriteVarString(optionalValue5);
        }
        writer.WriteVarUInt(checked((uint)ControlPoints.Count));
        foreach (var item7 in ControlPoints) {
            item7.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)ProgressKeyFrames.Count));
        foreach (var item9 in ProgressKeyFrames) {
            item9.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)RotationKeyFrames.Count));
        foreach (var item11 in RotationKeyFrames) {
            item11.Write(writer);
        }
    }
}
