using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class CreativeGroupInfoPayload : DataType {
    public CreativeItemCategory CreativeCategory;
    public string Name = string.Empty;
    public CreativeItemStack GroupIconItem = new();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8((byte)CreativeCategory);
        writer.WriteVarString(Name);
        GroupIconItem.Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        CreativeCategory = (CreativeItemCategory)reader.ReadUInt8();
        Name = reader.ReadVarString();
        GroupIconItem.Read(ref reader);
    }
}
