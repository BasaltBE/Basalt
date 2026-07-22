using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemDescriptorCount : DataType {
    /// <summary>
    /// Descriptor format type id.
    /// </summary>
    public byte DescriptorType;
    /// <summary>
    /// Item network id value.
    /// </summary>
    public short NetworkId;
    /// <summary>
    /// Item metadata value.
    /// </summary>
    public short MetadataValue;
    /// <summary>
    /// Descriptor text value.
    /// </summary>
    public string Text = string.Empty;
    /// <summary>
    /// Descriptor version value.
    /// </summary>
    public byte Version;
    /// <summary>
    /// Item count value.
    /// </summary>
    public int Count;
    public void Read(BinaryReader reader) {
        DescriptorType = reader.ReadUInt8();
        switch (DescriptorType) {
            case 1:
                NetworkId = reader.ReadInt16(true);
                if (NetworkId != 0) {
                    MetadataValue = reader.ReadInt16(true);
                }
                break;
            case 2:
                Text = reader.ReadVarString();
                Version = reader.ReadUInt8();
                break;
            case 3:
            case 5:
                Text = reader.ReadVarString();
                break;
            case 4:
                Text = reader.ReadVarString();
                MetadataValue = reader.ReadInt16(true);
                break;
        }

        Count = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8(DescriptorType);
        switch (DescriptorType) {
            case 1:
                writer.WriteInt16(NetworkId, true);
                if (NetworkId != 0) {
                    writer.WriteInt16(MetadataValue, true);
                }
                break;
            case 2:
                writer.WriteVarString(Text);
                writer.WriteUInt8(Version);
                break;
            case 3:
            case 5:
                writer.WriteVarString(Text);
                break;
            case 4:
                writer.WriteVarString(Text);
                writer.WriteInt16(MetadataValue, true);
                break;
        }

        writer.WriteZigZag(Count);
    }
}
