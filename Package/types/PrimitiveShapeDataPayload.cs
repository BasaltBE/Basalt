#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PrimitiveShapeDataPayload {
    public ulong NetworkId;
    public ScriptPrimitiveShapeType? ShapeType;
    public Vec3? Location;
    public float? Scale;
    public Vec3? Rotation;
    public float? TotalTimeLeft;
    public float? MaximumRenderDistance;
    public Color? Color;
    public DimensionType DimensionID = new();
    public ActorUniqueID? AttachedToEntityID;
    public PrimitiveShapeDataPayloadExtraShapeDataVariant? ExtraShapeData;

    public void Read(BinaryReader reader) {
        NetworkId = reader.ReadVarULong();
        if (reader.ReadBool()) {
            ShapeType = (global::BedrockProtocol.Enums.ScriptPrimitiveShapeType)reader.ReadUInt8();
        } else {
            ShapeType = default;
        }
        if (reader.ReadBool()) {
            Vec3 readValue4 = new();
            readValue4.Read(reader);
            Location = readValue4;
        } else {
            Location = default;
        }
        if (reader.ReadBool()) {
            Scale = reader.ReadF32(true);
        } else {
            Scale = default;
        }
        if (reader.ReadBool()) {
            Vec3 readValue8 = new();
            readValue8.Read(reader);
            Rotation = readValue8;
        } else {
            Rotation = default;
        }
        if (reader.ReadBool()) {
            TotalTimeLeft = reader.ReadF32(true);
        } else {
            TotalTimeLeft = default;
        }
        if (reader.ReadBool()) {
            MaximumRenderDistance = reader.ReadF32(true);
        } else {
            MaximumRenderDistance = default;
        }
        if (reader.ReadBool()) {
            Color readValue14 = new();
            readValue14.Read(reader);
            Color = readValue14;
        } else {
            Color = default;
        }
        DimensionID.Read(reader);
        if (reader.ReadBool()) {
            ActorUniqueID readValue18 = new();
            readValue18.Read(reader);
            AttachedToEntityID = readValue18;
        } else {
            AttachedToEntityID = default;
        }
        if (reader.ReadBool()) {
            uint variant20 = reader.ReadVarUInt();
            switch (variant20) {
                case 0:
                    ArrowDataPayload readValue3020 = new();
                    readValue3020.Read(reader);
                    ExtraShapeData = readValue3020;
                    break;
                case 1:
                    TextDataPayload readValue3021 = new();
                    readValue3021.Read(reader);
                    ExtraShapeData = readValue3021;
                    break;
                case 2:
                    BoxDataPayload readValue3022 = new();
                    readValue3022.Read(reader);
                    ExtraShapeData = readValue3022;
                    break;
                case 3:
                    LineDataPayload readValue3023 = new();
                    readValue3023.Read(reader);
                    ExtraShapeData = readValue3023;
                    break;
                case 4:
                    SphereDataPayload readValue3024 = new();
                    readValue3024.Read(reader);
                    ExtraShapeData = readValue3024;
                    break;
                case 5:
                    CylinderDataPayload readValue3025 = new();
                    readValue3025.Read(reader);
                    ExtraShapeData = readValue3025;
                    break;
                case 6:
                    PyramidDataPayload readValue3026 = new();
                    readValue3026.Read(reader);
                    ExtraShapeData = readValue3026;
                    break;
                case 7:
                    EllipsoidDataPayload readValue3027 = new();
                    readValue3027.Read(reader);
                    ExtraShapeData = readValue3027;
                    break;
                case 8:
                    ConeDataPayload readValue3028 = new();
                    readValue3028.Read(reader);
                    ExtraShapeData = readValue3028;
                    break;
                default:
                    throw new FormatException($"Unknown union variant {variant20} for ExtraShapeData.");
            }
        } else {
            ExtraShapeData = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(NetworkId);
        writer.WriteBool(ShapeType is not null);
        if (ShapeType is { } optionalValue3) {
            writer.WriteUInt8((byte)optionalValue3);
        }
        writer.WriteBool(Location is not null);
        if (Location is { } optionalValue5) {
            optionalValue5.Write(writer);
        }
        writer.WriteBool(Scale is not null);
        if (Scale is { } optionalValue7) {
            writer.WriteF32(optionalValue7, true);
        }
        writer.WriteBool(Rotation is not null);
        if (Rotation is { } optionalValue9) {
            optionalValue9.Write(writer);
        }
        writer.WriteBool(TotalTimeLeft is not null);
        if (TotalTimeLeft is { } optionalValue11) {
            writer.WriteF32(optionalValue11, true);
        }
        writer.WriteBool(MaximumRenderDistance is not null);
        if (MaximumRenderDistance is { } optionalValue13) {
            writer.WriteF32(optionalValue13, true);
        }
        writer.WriteBool(Color is not null);
        if (Color is { } optionalValue15) {
            optionalValue15.Write(writer);
        }
        DimensionID.Write(writer);
        writer.WriteBool(AttachedToEntityID is not null);
        if (AttachedToEntityID is { } optionalValue19) {
            optionalValue19.Write(writer);
        }
        writer.WriteBool(ExtraShapeData is not null);
        if (ExtraShapeData is { } optionalValue21) {
            switch (optionalValue21) {
                case ArrowDataPayload variantValue0:
                    writer.WriteVarUInt(0);
                    variantValue0.Write(writer);
                    break;
                case TextDataPayload variantValue1:
                    writer.WriteVarUInt(1);
                    variantValue1.Write(writer);
                    break;
                case BoxDataPayload variantValue2:
                    writer.WriteVarUInt(2);
                    variantValue2.Write(writer);
                    break;
                case LineDataPayload variantValue3:
                    writer.WriteVarUInt(3);
                    variantValue3.Write(writer);
                    break;
                case SphereDataPayload variantValue4:
                    writer.WriteVarUInt(4);
                    variantValue4.Write(writer);
                    break;
                case CylinderDataPayload variantValue5:
                    writer.WriteVarUInt(5);
                    variantValue5.Write(writer);
                    break;
                case PyramidDataPayload variantValue6:
                    writer.WriteVarUInt(6);
                    variantValue6.Write(writer);
                    break;
                case EllipsoidDataPayload variantValue7:
                    writer.WriteVarUInt(7);
                    variantValue7.Write(writer);
                    break;
                case ConeDataPayload variantValue8:
                    writer.WriteVarUInt(8);
                    variantValue8.Write(writer);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported union value for optionalValue21.");
            }
        }
    }
}
