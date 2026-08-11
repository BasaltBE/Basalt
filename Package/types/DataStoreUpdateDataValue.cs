using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public enum DataStoreUpdateDataValueKind : uint {
    DoubleValue = 0,
    BooleanValue = 1,
    StringValue = 2,
}

public sealed class DataStoreUpdateDataValue {
    public DataStoreUpdateDataValueKind Kind;
    public double DoubleValue;
    public bool BooleanValue;
    public string StringValue = string.Empty;

    public void Read(BinaryReader reader) {
        Kind = (DataStoreUpdateDataValueKind)reader.ReadVarUInt();
        switch (Kind) {
            case DataStoreUpdateDataValueKind.DoubleValue:
                DoubleValue = reader.ReadF64(true);
                break;
            case DataStoreUpdateDataValueKind.BooleanValue:
                BooleanValue = reader.ReadBool();
                break;
            case DataStoreUpdateDataValueKind.StringValue:
                StringValue = reader.ReadVarString();
                break;
            default:
                throw new FormatException($"Unknown DataStoreUpdateDataValue variant {Kind}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(((uint)Kind));
        switch (Kind) {
            case DataStoreUpdateDataValueKind.DoubleValue:
                writer.WriteF64(DoubleValue, true);
                break;
            case DataStoreUpdateDataValueKind.BooleanValue:
                writer.WriteBool(BooleanValue);
                break;
            case DataStoreUpdateDataValueKind.StringValue:
                writer.WriteVarString(StringValue);
                break;
            default:
                throw new InvalidOperationException($"Unsupported DataStoreUpdateDataValue variant {Kind}.");
        }
    }
}
