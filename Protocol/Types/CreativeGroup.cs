using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeGroup : DataType
{
    public int Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public CreativeItemInstanceDescriptor Icon { get; set; } = new();

    public void Read(ref BinaryReader reader)
    {
        Category = reader.ReadInt32(true);
        Name = reader.ReadVarString();
        Icon.Read(ref reader);
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteInt32(Category, true);
        writer.WriteVarString(Name);
        Icon.Write(ref writer);
    }
}
