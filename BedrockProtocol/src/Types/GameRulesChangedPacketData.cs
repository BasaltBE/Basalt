using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class GameRulesChangedPacketData : DataType {
    public GameRule[] Rules = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Rules.Length);
        foreach (GameRule rule in Rules)
            rule.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Rules = new GameRule[count];
        for (int index = 0; index < count; index++) {
            GameRule rule = new();
            rule.Read(ref reader);
            Rules[index] = rule;
        }
    }
}
