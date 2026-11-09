using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemTagDescriptorData : ItemStackItemDescriptorData {
    public override ItemDescriptorType DescriptorType => ItemDescriptorType.ItemTag;
    public string ItemTag = string.Empty;
    public override void Read(ref BinaryReader reader) => ItemTag = reader.ReadVarString();
    public override void Write(ref BinaryWriter writer) => writer.WriteVarString(ItemTag);
}
