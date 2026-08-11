using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public enum GameRuleRuleValueValueKind : uint {
    BooleanValue = 0,
    Int32Value = 1,
    FloatValue = 2,
}

public sealed class GameRuleRuleValueValue {
    public GameRuleRuleValueValueKind Kind;
    public bool BooleanValue;
    public int Int32Value;
    public float FloatValue;

    public void Read(BinaryReader reader) {
        Kind = (GameRuleRuleValueValueKind)reader.ReadVarUInt();
        switch (Kind) {
            case GameRuleRuleValueValueKind.BooleanValue:
                BooleanValue = reader.ReadBool();
                break;
            case GameRuleRuleValueValueKind.Int32Value:
                Int32Value = reader.ReadInt32(true);
                break;
            case GameRuleRuleValueValueKind.FloatValue:
                FloatValue = reader.ReadF32(true);
                break;
            default:
                throw new FormatException($"Unknown GameRuleRuleValueValue variant {Kind}.");
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(((uint)Kind));
        switch (Kind) {
            case GameRuleRuleValueValueKind.BooleanValue:
                writer.WriteBool(BooleanValue);
                break;
            case GameRuleRuleValueValueKind.Int32Value:
                writer.WriteInt32(Int32Value, true);
                break;
            case GameRuleRuleValueValueKind.FloatValue:
                writer.WriteF32(FloatValue, true);
                break;
            default:
                throw new InvalidOperationException($"Unsupported GameRuleRuleValueValue variant {Kind}.");
        }
    }
}
