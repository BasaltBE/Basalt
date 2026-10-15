using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CreativeGroupInfoPayload : DataType {
    public CreativeItemCategory CreativeCategory;
    public string Name = string.Empty;
    public NetworkItemInstanceDescriptor GroupIconItem = new();

    public override void Read(ref BinaryReader reader) {
        CreativeCategory = (CreativeItemCategory)reader.ReadUInt8();
        Name = reader.ReadVarString();
        GroupIconItem.Read(ref reader);
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)CreativeCategory);
        writer.WriteVarString(Name);
        GroupIconItem.Write(ref writer);
    }
}
