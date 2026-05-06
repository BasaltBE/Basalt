using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class GameRule
{
    public string Name { get; set; } = string.Empty;
    public bool CanBeModifiedByPlayer { get; set; }
    public object Value { get; set; } = false;

    public void ReadLegacy(ref BinaryReader reader)
    {
        Name = reader.ReadVarString();
        CanBeModifiedByPlayer = reader.ReadBool();
        GameRuleValueType type = (GameRuleValueType)reader.ReadVarUInt();

        Value = type switch
        {
            GameRuleValueType.Bool => reader.ReadBool(),
            GameRuleValueType.Int => reader.ReadVarUInt(),
            GameRuleValueType.Float => reader.ReadF32(true),
            _ => throw new InvalidOperationException($"Unknown game rule value type: {type}.")
        };
    }

    public void WriteLegacy(ref BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteBool(CanBeModifiedByPlayer);

        switch (Value)
        {
            case bool boolValue:
                writer.WriteVarUInt((uint)GameRuleValueType.Bool);
                writer.WriteBool(boolValue);
                break;
            case byte byteValue:
                writer.WriteVarUInt((uint)GameRuleValueType.Int);
                writer.WriteVarUInt(byteValue);
                break;
            case ushort ushortValue:
                writer.WriteVarUInt((uint)GameRuleValueType.Int);
                writer.WriteVarUInt(ushortValue);
                break;
            case uint uintValue:
                writer.WriteVarUInt((uint)GameRuleValueType.Int);
                writer.WriteVarUInt(uintValue);
                break;
            case int intValue when intValue >= 0:
                writer.WriteVarUInt((uint)GameRuleValueType.Int);
                writer.WriteVarUInt((uint)intValue);
                break;
            case float floatValue:
                writer.WriteVarUInt((uint)GameRuleValueType.Float);
                writer.WriteF32(floatValue, true);
                break;
            default:
                throw new InvalidOperationException($"Unsupported game rule value type: {Value.GetType().FullName}.");
        }
    }
}
