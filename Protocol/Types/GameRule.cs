using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class GameRule : DataType
{
    /// <summary>
    /// Name of the game rule.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Whether the game rule can be modified by the player or not.
    /// </summary>
    public bool CanBeModifiedByPlayer;

    /// <summary>
    /// Value of the game rule. The type of this value is determined by the GameRuleValueType enum.
    /// </summary>
    public object Value = false;

    public void Read(BinaryReader reader)
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

    public void Write(BinaryWriter writer)
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

