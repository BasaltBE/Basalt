using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(108)]
public sealed class SetScorePacket : DataPacket {
    public ScorePacketEntry[] Entries = [];

    public override void Deserialize(ref BinaryReader reader) {
        Entries = new ScorePacketEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Entries.Length; i++)
            Entries[i] = ScorePacketEntry.ReadEntry(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Entries.Length);
        foreach (ScorePacketEntry entry in Entries)
            entry.WriteEntry(ref writer);
    }
}
