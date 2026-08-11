using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class GameRule {
    public string RuleName = string.Empty;
    public bool RuleCanBeModified;
    public GameRuleRuleValueValue? RuleValue;

    public void Read(BinaryReader reader) {
        RuleName = reader.ReadVarString();
        RuleCanBeModified = reader.ReadBool();
        if (reader.ReadBool()) {
            GameRuleRuleValueValue readValue4 = new();
            readValue4.Read(reader);
            RuleValue = readValue4;
        } else {
            RuleValue = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(RuleName);
        writer.WriteBool(RuleCanBeModified);
        writer.WriteBool(RuleValue is not null);
        if (RuleValue is { } optionalValue5) {
            optionalValue5.Write(writer);
        }
    }
}
