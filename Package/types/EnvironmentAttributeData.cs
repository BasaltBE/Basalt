using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class EnvironmentAttributeData {
    public string AttributeName = string.Empty;
    public EnvironmentAttributeDataFromAttributeVariant? FromAttribute;
    public EnvironmentAttributeDataAttributeVariant Attribute = null!;
    public EnvironmentAttributeDataToAttributeVariant? ToAttribute;
    public uint CurrentTransitionTicks;
    public uint TotalTransitionTicks;
    public easing_function Easing;
    public uint LocalTransitionTicks;
    public bool NoiseTransition;

    public void Read(BinaryReader reader) {
        AttributeName = reader.ReadVarString();
        if (reader.ReadBool()) {
            uint variant2 = reader.ReadVarUInt();
            switch (variant2) {
                case 0:
                    BoolAttributeData readValue3002 = new();
                    readValue3002.Read(reader);
                    FromAttribute = readValue3002;
                    break;
                case 1:
                    FloatAttributeData readValue3003 = new();
                    readValue3003.Read(reader);
                    FromAttribute = readValue3003;
                    break;
                case 2:
                    ColorAttributeData readValue3004 = new();
                    readValue3004.Read(reader);
                    FromAttribute = readValue3004;
                    break;
                default:
                    throw new FormatException($"Unknown union variant {variant2} for FromAttribute.");
            }
        } else {
            FromAttribute = default;
        }
        uint variant4 = reader.ReadVarUInt();
        switch (variant4) {
            case 0:
                BoolAttributeData readValue3004 = new();
                readValue3004.Read(reader);
                Attribute = readValue3004;
                break;
            case 1:
                FloatAttributeData readValue3005 = new();
                readValue3005.Read(reader);
                Attribute = readValue3005;
                break;
            case 2:
                ColorAttributeData readValue3006 = new();
                readValue3006.Read(reader);
                Attribute = readValue3006;
                break;
            default:
                throw new FormatException($"Unknown union variant {variant4} for Attribute.");
        }
        if (reader.ReadBool()) {
            uint variant6 = reader.ReadVarUInt();
            switch (variant6) {
                case 0:
                    BoolAttributeData readValue3006 = new();
                    readValue3006.Read(reader);
                    ToAttribute = readValue3006;
                    break;
                case 1:
                    FloatAttributeData readValue3007 = new();
                    readValue3007.Read(reader);
                    ToAttribute = readValue3007;
                    break;
                case 2:
                    ColorAttributeData readValue3008 = new();
                    readValue3008.Read(reader);
                    ToAttribute = readValue3008;
                    break;
                default:
                    throw new FormatException($"Unknown union variant {variant6} for ToAttribute.");
            }
        } else {
            ToAttribute = default;
        }
        CurrentTransitionTicks = reader.ReadUInt32(true);
        TotalTransitionTicks = reader.ReadUInt32(true);
        Easing = (global::BedrockProtocol.Enums.easing_function)reader.ReadInt32(true);
        LocalTransitionTicks = reader.ReadUInt32(true);
        NoiseTransition = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(AttributeName);
        writer.WriteBool(FromAttribute is not null);
        if (FromAttribute is { } optionalValue3) {
            switch (optionalValue3) {
                case BoolAttributeData variantValue0:
                    writer.WriteVarUInt(0);
                    variantValue0.Write(writer);
                    break;
                case FloatAttributeData variantValue1:
                    writer.WriteVarUInt(1);
                    variantValue1.Write(writer);
                    break;
                case ColorAttributeData variantValue2:
                    writer.WriteVarUInt(2);
                    variantValue2.Write(writer);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported union value for optionalValue3.");
            }
        }
        switch (Attribute) {
            case BoolAttributeData variantValue0:
                writer.WriteVarUInt(0);
                variantValue0.Write(writer);
                break;
            case FloatAttributeData variantValue1:
                writer.WriteVarUInt(1);
                variantValue1.Write(writer);
                break;
            case ColorAttributeData variantValue2:
                writer.WriteVarUInt(2);
                variantValue2.Write(writer);
                break;
            default:
                throw new InvalidOperationException("Unsupported union value for Attribute.");
        }
        writer.WriteBool(ToAttribute is not null);
        if (ToAttribute is { } optionalValue7) {
            switch (optionalValue7) {
                case BoolAttributeData variantValue0:
                    writer.WriteVarUInt(0);
                    variantValue0.Write(writer);
                    break;
                case FloatAttributeData variantValue1:
                    writer.WriteVarUInt(1);
                    variantValue1.Write(writer);
                    break;
                case ColorAttributeData variantValue2:
                    writer.WriteVarUInt(2);
                    variantValue2.Write(writer);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported union value for optionalValue7.");
            }
        }
        writer.WriteUInt32(CurrentTransitionTicks, true);
        writer.WriteUInt32(TotalTransitionTicks, true);
        writer.WriteInt32((int)Easing, true);
        writer.WriteUInt32(LocalTransitionTicks, true);
        writer.WriteBool(NoiseTransition);
    }
}
