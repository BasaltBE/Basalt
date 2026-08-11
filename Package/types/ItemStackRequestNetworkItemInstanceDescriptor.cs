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
        ItemDescriptorType variant0 = (global::BedrockProtocol.Enums.ItemDescriptorType)reader.ReadUInt8();
        switch (variant0) {
            case global::BedrockProtocol.Enums.ItemDescriptorType.Empty: {
                EmptyItemDescriptor variantValue0_0 = new();
                ItemDescriptor = variantValue0_0;
                break;
            }
            case global::BedrockProtocol.Enums.ItemDescriptorType.ItemName: {
                ItemNameDescriptor variantValue0_1 = new();
                variantValue0_1.FullName = reader.ReadVarString();
                variantValue0_1.AuxValue = reader.ReadZigZag();
                ItemDescriptor = variantValue0_1;
                break;
            }
            case global::BedrockProtocol.Enums.ItemDescriptorType.Molang: {
                MolangItemDescriptor variantValue0_2 = new();
                variantValue0_2.TagExpression = reader.ReadVarString();
                variantValue0_2.MolangVersion = (global::BedrockProtocol.Enums.MolangVersion)reader.ReadInt16(true);
                ItemDescriptor = variantValue0_2;
                break;
            }
            case global::BedrockProtocol.Enums.ItemDescriptorType.ItemTag: {
                ItemTagDescriptor variantValue0_3 = new();
                variantValue0_3.ItemTag = reader.ReadVarString();
                ItemDescriptor = variantValue0_3;
                break;
            }
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
                variantValue0.Write(writer);
                break;
            case ItemNameDescriptor variantValue1:
                variantValue1.Write(writer);
                break;
            case MolangItemDescriptor variantValue2:
                variantValue2.Write(writer);
                break;
            case ItemTagDescriptor variantValue3:
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
