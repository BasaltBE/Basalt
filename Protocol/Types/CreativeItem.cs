using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeItem : DataType
{
    /// <summary>
    /// Item index in creative content.
    /// </summary>
    public int ItemIndex;

    /// <summary>
    /// Item descriptor payload.
    /// </summary>
    public CreativeItemInstanceDescriptor ItemInstance = new();

    /// <summary>
    /// Creative group index.
    /// </summary>
    public int GroupIndex;

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
