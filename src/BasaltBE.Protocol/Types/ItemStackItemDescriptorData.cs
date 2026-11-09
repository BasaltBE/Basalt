using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public abstract class ItemStackItemDescriptorData : DataType {
    public abstract ItemDescriptorType DescriptorType { get; }

    public static ItemStackItemDescriptorData ReadDescriptor(ref BinaryReader reader) {
        reader.ReadVarUInt();
        ItemDescriptorType type = (ItemDescriptorType)reader.ReadUInt8();
        ItemStackItemDescriptorData descriptor = type switch {
            ItemDescriptorType.Empty => new EmptyItemDescriptorData(),
            ItemDescriptorType.ItemName => new ItemNameDescriptorData(),
            ItemDescriptorType.Molang => new MolangItemDescriptorData(),
            ItemDescriptorType.ItemTag => new ItemTagDescriptorData(),
            _ => throw new InvalidDataException($"Unknown item descriptor type {(byte)type}.")
        };
        descriptor.Read(ref reader);
        return descriptor;
    }

    public void WriteDescriptor(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)DescriptorType);
        writer.WriteUInt8((byte)DescriptorType);
        Write(ref writer);
    }
}
