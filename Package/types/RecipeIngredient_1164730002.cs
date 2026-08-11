using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class RecipeIngredient_1164730002 {
    public ItemDescriptorType DescriptorType;
    public short AuxValue;
    public string Text = string.Empty;
    public MolangVersion MolangVersion;
    public int StackSize = 1;

    public void Read(BinaryReader reader) {
        uint variant = reader.ReadVarUInt();

        DescriptorType = variant switch {
            0 => ItemDescriptorType.Empty,
            1 => reader.ReadVarString() switch {
                "name" => ItemDescriptorType.ItemName,
                "molang" => ItemDescriptorType.Molang,
                "item_tag" => ItemDescriptorType.ItemTag,
                string value => throw new FormatException($"Unsupported item descriptor name: {value}.")
            },
            _ => throw new FormatException($"Invalid item descriptor variant: {variant}.")
        };

        switch (DescriptorType) {
            case ItemDescriptorType.Empty:
                AuxValue = checked((short)reader.ReadZigZag());
                break;

            case ItemDescriptorType.ItemName:
                Text = reader.ReadVarString();
                AuxValue = checked((short)reader.ReadZigZag());
                break;

            case ItemDescriptorType.Molang:
                Text = reader.ReadVarString();
                MolangVersion = (MolangVersion)reader.ReadInt16(true);
                break;

            case ItemDescriptorType.ItemTag:
                Text = reader.ReadVarString();
                AuxValue = checked((short)reader.ReadZigZag());
                break;

            default:
                throw new FormatException($"Unsupported item descriptor type: {DescriptorType}.");
        }

        StackSize = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        if (StackSize is < 1 or > ushort.MaxValue) {
            throw new InvalidOperationException($"Invalid ingredient count: {StackSize}.");
        }

        switch (DescriptorType) {
            case ItemDescriptorType.Empty:
                writer.WriteVarUInt(0);
                writer.WriteZigZag(32767);
                break;

            case ItemDescriptorType.ItemName:
                if (string.IsNullOrEmpty(Text)) {
                    throw new InvalidOperationException("Item-name descriptor requires a full item name.");
                }
                writer.WriteVarUInt(1);
                writer.WriteVarString("name");
                writer.WriteVarString(Text);
                writer.WriteZigZag(AuxValue);
                break;

            case ItemDescriptorType.Molang:
                writer.WriteVarUInt(1);
                writer.WriteVarString("molang");
                writer.WriteVarString(Text);
                writer.WriteInt16((short)MolangVersion, true);
                break;

            case ItemDescriptorType.ItemTag:
                writer.WriteVarUInt(1);
                writer.WriteVarString("item_tag");
                writer.WriteVarString(Text);
                writer.WriteZigZag(AuxValue);
                break;

            default:
                throw new InvalidOperationException($"Unsupported item descriptor type: {DescriptorType}.");
        }

        writer.WriteZigZag(StackSize);
    }
}
