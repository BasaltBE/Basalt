using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(145)]
public sealed class CreativeContentPacket : DataPacket {
    public CreativeGroupInfoPayload[] Groups = [];
    public CreativeItemEntryPayload[] Entries = [];

    public override void Deserialize(ref BinaryReader reader) {
        Groups = new CreativeGroupInfoPayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Groups.Length; i++) {
            CreativeGroupInfoPayload group = new();
            group.Read(ref reader);
            Groups[i] = group;
        }

        Entries = new CreativeItemEntryPayload[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Entries.Length; i++) {
            CreativeItemEntryPayload entry = new();
            entry.Read(ref reader);
            Entries[i] = entry;
        }
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Groups.Length);
        foreach (CreativeGroupInfoPayload group in Groups)
            group.Write(ref writer);

        writer.WriteVarUInt((uint)Entries.Length);
        foreach (CreativeItemEntryPayload entry in Entries)
            entry.Write(ref writer);
    }
}
