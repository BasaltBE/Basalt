using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(145)]
public sealed class CreativeContentPacket : DataPacket {
    public CreativeGroupInfoPayload[] Groups = Array.Empty<CreativeGroupInfoPayload>();
    public CreativeItemEntryPayload[] Entries = Array.Empty<CreativeItemEntryPayload>();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Groups.Length);

        foreach (CreativeGroupInfoPayload group in Groups) {
            group.Write(ref writer);
        }

        writer.WriteVarUInt((uint)Entries.Length);

        foreach (CreativeItemEntryPayload entry in Entries) {
            entry.Write(ref writer);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        int groupCount = checked((int)reader.ReadVarUInt());
        Groups = new CreativeGroupInfoPayload[groupCount];

        for (int index = 0; index < groupCount; index++) {
            CreativeGroupInfoPayload group = new();
            group.Read(ref reader);
            Groups[index] = group;
        }

        int entryCount = checked((int)reader.ReadVarUInt());
        Entries = new CreativeItemEntryPayload[entryCount];

        for (int index = 0; index < entryCount; index++) {
            CreativeItemEntryPayload entry = new();
            entry.Read(ref reader);
            Entries[index] = entry;
        }
    }
}
