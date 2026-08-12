#nullable enable

using System;
using BedrockProtocol.Enums;
using BedrockProtocol.Nbt;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataItemEntry {
    private static readonly TagOptions NetworkNbtOptions = new(Name: true, Type: true, VarInt: true);

    public uint ID;
    public DataItemEntryPayloadVariant Payload = null!;

    public void Read(BinaryReader reader) {
        ID = reader.ReadVarUInt();
        uint variant2 = reader.ReadVarUInt();
        switch (variant2) {
            case 0:
                DataItemBytePayload readValue3002 = new();
                readValue3002.Read(reader);
                Payload = readValue3002;
                break;
            case 1:
                DataItemShortPayload readValue3003 = new();
                readValue3003.Read(reader);
                Payload = readValue3003;
                break;
            case 2:
                DataItemIntPayload readValue3004 = new();
                readValue3004.Read(reader);
                Payload = readValue3004;
                break;
            case 3:
                DataItemFloatPayload readValue3005 = new();
                readValue3005.Read(reader);
                Payload = readValue3005;
                break;
            case 4:
                DataItemStringPayload readValue3006 = new();
                readValue3006.Read(reader);
                Payload = readValue3006;
                break;
            case 5:
                DataItemCompoundTagPayload readValue3007 = new();
                readValue3007.Read(reader);
                Payload = readValue3007;
                break;
            case 6:
                DataItemPosPayload readValue3008 = new();
                readValue3008.Read(reader);
                Payload = readValue3008;
                break;
            case 7:
                DataItemInt64Payload readValue3009 = new();
                readValue3009.Read(reader);
                Payload = readValue3009;
                break;
            case 8:
                DataItemVec3Payload readValue3010 = new();
                readValue3010.Read(reader);
                Payload = readValue3010;
                break;
            default:
                throw new FormatException($"Unknown union variant {variant2} for Payload.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(ID);
        switch (Payload) {
            case DataItemBytePayload variantValue0:
                writer.WriteVarUInt(0);
                variantValue0.Write(writer);
                break;
            case DataItemShortPayload variantValue1:
                writer.WriteVarUInt(1);
                variantValue1.Write(writer);
                break;
            case DataItemIntPayload variantValue2:
                writer.WriteVarUInt(2);
                variantValue2.Write(writer);
                break;
            case DataItemFloatPayload variantValue3:
                writer.WriteVarUInt(3);
                variantValue3.Write(writer);
                break;
            case DataItemStringPayload variantValue4:
                writer.WriteVarUInt(4);
                variantValue4.Write(writer);
                break;
            case DataItemCompoundTagPayload variantValue5:
                writer.WriteVarUInt(5);
                variantValue5.Write(writer);
                break;
            case DataItemPosPayload variantValue6:
                writer.WriteVarUInt(6);
                variantValue6.Write(writer);
                break;
            case DataItemInt64Payload variantValue7:
                writer.WriteVarUInt(7);
                variantValue7.Write(writer);
                break;
            case DataItemVec3Payload variantValue8:
                writer.WriteVarUInt(8);
                variantValue8.Write(writer);
                break;
            default:
                throw new InvalidOperationException("Unsupported union value for Payload.");
        }
    }
}
