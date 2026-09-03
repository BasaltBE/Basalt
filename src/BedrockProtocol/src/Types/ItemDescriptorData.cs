using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class ItemDescriptorData : DataType {
    public ItemDescriptorType Type;
    public string Name = string.Empty;
    public int AuxValue;
    public MolangVersion MolangVersion;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Type);
        writer.WriteUInt8((byte)Type);
        switch (Type) {
            case ItemDescriptorType.ItemName:
                writer.WriteVarString(Name);
                writer.WriteVarInt(AuxValue);
                break;
            case ItemDescriptorType.Molang:
                writer.WriteVarString(Name);
                writer.WriteInt16((short)MolangVersion, true);
                break;
            case ItemDescriptorType.ItemTag:
                writer.WriteVarString(Name);
                break;
            case ItemDescriptorType.Empty:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(Type));
        }
    }

    public override void Read(ref BinaryReader reader) {
        reader.ReadVarUInt();
        Type = (ItemDescriptorType)reader.ReadUInt8();
        switch (Type) {
            case ItemDescriptorType.ItemName:
                Name = reader.ReadVarString();
                AuxValue = reader.ReadVarInt();
                break;
            case ItemDescriptorType.Molang:
                Name = reader.ReadVarString();
                MolangVersion = (MolangVersion)reader.ReadInt16(true);
                break;
            case ItemDescriptorType.ItemTag:
                Name = reader.ReadVarString();
                break;
            case ItemDescriptorType.Empty:
                break;
            default:
                throw new FormatException("Unsupported item descriptor type.");
        }
    }
}
