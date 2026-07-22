using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class CreativeGroup : DataType {
    /// <summary>
    /// Creative category id.
    /// </summary>
    public int Category;

    /// <summary>
    /// Group name.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// Group icon item descriptor.
    /// </summary>
    public LegacyNetworkItemStackDescriptor Icon = new();

    public void Read(BinaryReader reader) {
        Category = reader.ReadInt32(true);
        Name = reader.ReadVarString();
        Icon.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(Category, true);
        writer.WriteVarString(Name);
        Icon.Write(writer);
    }
}
