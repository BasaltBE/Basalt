using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(108)]
public sealed class SetScorePacket : DataPacket {
    public ScoreEntry[] Entries = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Entries.Length);
        for (int i = 0; i < Entries.Length; i++) Entries[i].Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Entries = new ScoreEntry[count];
        for (int i = 0; i < count; i++) {
            Entries[i] = new ScoreEntry();
            Entries[i].Read(ref reader);
        }
    }
}
