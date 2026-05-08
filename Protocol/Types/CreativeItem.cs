using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeItem : DataType
{
    public int ItemIndex { get; set; }
    public CreativeItemInstanceDescriptor ItemInstance { get; set; } = new();
    public int GroupIndex { get; set; }

    public void Read(ref BinaryReader reader)
    {
        ItemIndex = reader.ReadVarInt();
        ItemInstance.Read(ref reader);
        GroupIndex = reader.ReadVarInt();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteVarInt(ItemIndex);
        ItemInstance.Write(ref writer);
        writer.WriteVarInt(GroupIndex);
    }
}
