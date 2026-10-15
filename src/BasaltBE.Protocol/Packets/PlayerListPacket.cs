using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(63)]
public sealed class PlayerListPacket : DataPacket {
    public PlayerListEntry[] Entries = [];

    public override void Deserialize(ref BinaryReader reader) {
        Entries = new PlayerListEntry[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Entries.Length; i++) {
            Entries[i] = new PlayerListEntry();
            Entries[i].Read(ref reader);
        }
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Entries.Length);
        foreach (PlayerListEntry entry in Entries)
            entry.Write(ref writer);
    }
}
