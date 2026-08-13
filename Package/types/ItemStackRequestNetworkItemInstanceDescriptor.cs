#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ItemStackRequestNetworkItemInstanceDescriptor {
    public ItemStackRequestNetworkItemInstanceDescriptorItemDescriptorVariant ItemDescriptor = null!;
    public ushort StackSize;
    public uint BlockRuntimeId;
    public byte[] UserDataBuffer = [];

    public void Read(BinaryReader reader) {
        uint variant0 = reader.ReadVarUInt();
        switch (variant0) {
            case 0:
                EmptyItemDescriptor readValue3000 = new();
                readValue3000.Read(reader);
                ItemDescriptor = readValue3000;
                break;
            case 1:
                ItemNameDescriptor readValue3001 = new();
                readValue3001.Read(reader);
                ItemDescriptor = readValue3001;
                break;
            case 2:
                MolangItemDescriptor readValue3002 = new();
                readValue3002.Read(reader);
                ItemDescriptor = readValue3002;
                break;
            case 3:
                ItemTagDescriptor readValue3003 = new();
                readValue3003.Read(reader);
                ItemDescriptor = readValue3003;
                break;
            default:
                throw new FormatException($"Unknown union variant {variant0} for ItemDescriptor.");
        }
        StackSize = reader.ReadUInt16(true);
        BlockRuntimeId = reader.ReadVarUInt();
        int binaryLength6 = checked((int)reader.ReadVarUInt());
        UserDataBuffer = reader.ReadBytes(binaryLength6).ToArray();
    }

    public void Write(BinaryWriter writer) {
        switch (ItemDescriptor) {
            case EmptyItemDescriptor variantValue0:
                writer.WriteVarUInt(0);
                variantValue0.Write(writer);
                break;
            case ItemNameDescriptor variantValue1:
                writer.WriteVarUInt(1);
                variantValue1.Write(writer);
                break;
            case MolangItemDescriptor variantValue2:
                writer.WriteVarUInt(2);
                variantValue2.Write(writer);
                break;
            case ItemTagDescriptor variantValue3:
                writer.WriteVarUInt(3);
                variantValue3.Write(writer);
                break;
            default:
                throw new InvalidOperationException("Unsupported union value for ItemDescriptor.");
        }
        writer.WriteUInt16(StackSize, true);
        writer.WriteVarUInt(BlockRuntimeId);
        writer.WriteVarUInt(checked((uint)UserDataBuffer.Length));
        writer.WriteBytes(UserDataBuffer);
    }
}
