using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class RecipeIngredientData : DataType {
    public ItemStackItemDescriptorData ItemDescriptor = new EmptyItemDescriptorData();
    public ushort StackSize;
    public override void Read(ref BinaryReader reader) { ItemDescriptor = ItemStackItemDescriptorData.ReadDescriptor(ref reader); StackSize = reader.ReadUInt16(true); }
    public override void Write(ref BinaryWriter writer) { ItemDescriptor.WriteDescriptor(ref writer); writer.WriteUInt16(StackSize, true); }
}
