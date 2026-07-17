using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetDisplayObjective)]
public sealed record SetDisplayObjectivePacket : DataPacket
{
    public DisplaySlotType DisplaySlot;
    public string ObjectiveName = string.Empty;
    public string DisplayName = string.Empty;
    public string CriteriaName = "dummy";
    public ObjectiveSortOrder SortOrder;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        DisplaySlot = DisplaySlotTypeExtensions.FromProtocolString(reader.ReadVarString());
        ObjectiveName = reader.ReadVarString();
        DisplayName = reader.ReadVarString();
        CriteriaName = reader.ReadVarString();
        SortOrder = (ObjectiveSortOrder)reader.ReadZigZag();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarString(DisplaySlot.ToProtocolString());
        writer.WriteVarString(ObjectiveName);
        writer.WriteVarString(DisplayName);
        writer.WriteVarString(CriteriaName);
        writer.WriteZigZag((int)SortOrder);
    }
}
