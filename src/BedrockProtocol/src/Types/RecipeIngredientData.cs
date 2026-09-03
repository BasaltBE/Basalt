using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class RecipeIngredientData : DataType {
    public ItemDescriptorData Descriptor = new();
    public ushort StackSize;

    public override void Write(ref BinaryWriter writer) {
        Descriptor.Write(ref writer);
        writer.WriteUInt16(StackSize, true);
    }

    public override void Read(ref BinaryReader reader) {
        Descriptor.Read(ref reader);
        StackSize = reader.ReadUInt16(true);
    }
}
