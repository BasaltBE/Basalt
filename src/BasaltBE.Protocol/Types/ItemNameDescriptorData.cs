using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ItemNameDescriptorData : ItemStackItemDescriptorData {
    public override ItemDescriptorType DescriptorType => ItemDescriptorType.ItemName;
    public string FullName = string.Empty;
    public int AuxValue;
    public override void Read(ref BinaryReader reader) { FullName = reader.ReadVarString(); AuxValue = reader.ReadVarInt(); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarString(FullName); writer.WriteVarInt(AuxValue); }
}
