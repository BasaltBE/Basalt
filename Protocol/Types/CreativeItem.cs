using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeItem : DataType
{
    public int ItemIndex { get; set; }
    public CreativeItemInstanceDescriptor ItemInstance { get; set; } = new();
    public int GroupIndex { get; set; }

    public void Read(BinaryReader reader)
    {
        ItemIndex = reader.ReadVarInt();
        ItemInstance.Read(reader);
        GroupIndex = reader.ReadVarInt();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarInt(ItemIndex);
        ItemInstance.Write(writer);
        writer.WriteVarInt(GroupIndex);
    }
}
