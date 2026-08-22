using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class RecipeIngredient : DataType {
    public string Descriptor = string.Empty;
    public string DescriptorValue = string.Empty;
    public int AuxValue;
    public int StackSize;

    public override void Write(ref BinaryWriter writer) {
        if (Descriptor.Length == 0) {
            writer.WriteVarUInt(0);
            writer.WriteZigZag(32767);
        } else {
            writer.WriteVarUInt(1);
            writer.WriteVarString(Descriptor);
            writer.WriteVarString(DescriptorValue);
            writer.WriteZigZag(AuxValue);
        }

        writer.WriteZigZag(StackSize);
    }

    public override void Read(ref BinaryReader reader) {
        uint variant = reader.ReadVarUInt();
        if (variant == 0) {
            Descriptor = string.Empty;
            DescriptorValue = string.Empty;
            AuxValue = reader.ReadZigZag();
        } else if (variant == 1) {
            Descriptor = reader.ReadVarString();
            DescriptorValue = reader.ReadVarString();
            AuxValue = reader.ReadZigZag();
        } else {
            throw new FormatException($"Invalid recipe ingredient descriptor variant: {variant}.");
        }

        StackSize = reader.ReadZigZag();
    }
}
