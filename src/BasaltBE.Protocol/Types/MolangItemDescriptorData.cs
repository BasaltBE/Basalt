using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class MolangItemDescriptorData : ItemStackItemDescriptorData {
    public override ItemDescriptorType DescriptorType => ItemDescriptorType.Molang;
    public string TagExpression = string.Empty;
    public MolangVersion Version;
    public override void Read(ref BinaryReader reader) { TagExpression = reader.ReadVarString(); Version = (MolangVersion)reader.ReadInt16(true); }
    public override void Write(ref BinaryWriter writer) { writer.WriteVarString(TagExpression); writer.WriteInt16((short)Version, true); }
}
