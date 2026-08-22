using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(107)]
public sealed class SetDisplayObjectivePacket : DataPacket {
    public string DisplaySlotName = string.Empty;
    public string ObjectiveName = string.Empty;
    public string ObjectiveDisplayName = string.Empty;
    public string CriteriaName = string.Empty;
    public int SortOrder;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(DisplaySlotName);
        writer.WriteVarString(ObjectiveName);
        writer.WriteVarString(ObjectiveDisplayName);
        writer.WriteVarString(CriteriaName);
        writer.WriteVarInt(SortOrder);
    }

    public override void Deserialize(ref BinaryReader reader) {
        DisplaySlotName = reader.ReadVarString();
        ObjectiveName = reader.ReadVarString();
        ObjectiveDisplayName = reader.ReadVarString();
        CriteriaName = reader.ReadVarString();
        SortOrder = reader.ReadVarInt();
    }
}
