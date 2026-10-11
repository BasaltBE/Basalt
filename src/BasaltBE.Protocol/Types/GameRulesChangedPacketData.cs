using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class GameRulesChangedPacketData : DataType {
    public GameRule[] Rules = [];

    public override void Read(ref BinaryReader reader) {
        uint count = reader.ReadVarUInt();
        Rules = new GameRule[(int)count];
        for (int i = 0; i < Rules.Length; i++) {
            GameRule rule = new();
            rule.Read(ref reader);
            Rules[i] = rule;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Rules.Length);
        foreach (GameRule rule in Rules)
            rule.Write(ref writer);
    }
}
