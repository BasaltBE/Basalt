using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Basalt.BedrockProtocol.Enums;

namespace Basalt.BedrockProtocol.Types;

public sealed class GameRule : DataType {
    public string Name = string.Empty;
    public bool CanBeModified;
    public GameRuleValueType? ValueType;
    public bool BoolValue;
    public int IntValue;
    public float FloatValue;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteBool(CanBeModified);
        writer.WriteVarUInt((uint)(ValueType ?? GameRuleValueType.None));
        switch (ValueType) {
            case null:
                break;
            case GameRuleValueType.Bool:
                writer.WriteBool(BoolValue);
                break;
            case GameRuleValueType.Int:
                writer.WriteUInt32((uint)IntValue, true);
                break;
            case GameRuleValueType.Float:
                writer.WriteF32(FloatValue, true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ValueType));
        }
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        CanBeModified = reader.ReadBool();
        ValueType = (GameRuleValueType)reader.ReadVarUInt();
        if (ValueType == GameRuleValueType.None) {
            ValueType = null;
            return;
        }

        switch (ValueType) {
            case GameRuleValueType.Bool:
                BoolValue = reader.ReadBool();
                break;
            case GameRuleValueType.Int:
                IntValue = (int)reader.ReadUInt32(true);
                break;
            case GameRuleValueType.Float:
                FloatValue = reader.ReadF32(true);
                break;
            default:
                throw new FormatException("Unsupported game rule value type.");
        }
    }
}
