using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class GameRule : DataType {
    public string Name = string.Empty;
    public bool CanBeModified;
    public GameRuleValueType ValueType;
    public bool BooleanValue;
    public int IntegerValue;
    public float FloatValue;

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        CanBeModified = reader.ReadBool();
        ValueType = (GameRuleValueType)reader.ReadVarUInt();

        switch (ValueType) {
            case GameRuleValueType.None:
                break;
            case GameRuleValueType.Boolean:
                BooleanValue = reader.ReadBool();
                break;
            case GameRuleValueType.Integer:
                IntegerValue = reader.ReadInt32(true);
                break;
            case GameRuleValueType.Float:
                FloatValue = reader.ReadF32(true);
                break;
            default:
                throw new InvalidDataException($"Unknown game rule value type {(uint)ValueType}.");
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteBool(CanBeModified);
        writer.WriteVarUInt((uint)ValueType);

        switch (ValueType) {
            case GameRuleValueType.None:
                break;
            case GameRuleValueType.Boolean:
                writer.WriteBool(BooleanValue);
                break;
            case GameRuleValueType.Integer:
                writer.WriteInt32(IntegerValue, true);
                break;
            case GameRuleValueType.Float:
                writer.WriteF32(FloatValue, true);
                break;
            default:
                throw new InvalidDataException($"Unknown game rule value type {(uint)ValueType}.");
        }
    }
}
