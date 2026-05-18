using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ItemDescriptorCount : DataType
{
    public byte DescriptorType { get; set; }
    public short NetworkId { get; set; }
    public short MetadataValue { get; set; }
    public string Text { get; set; } = string.Empty;
    public byte Version { get; set; }
    public int Count { get; set; }

    public void Read(BinaryReader reader)
    {
        DescriptorType = reader.ReadUInt8();
        switch (DescriptorType)
        {
            case 1:
                NetworkId = reader.ReadInt16(true);
                if (NetworkId != 0)
                {
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

    public void Write(BinaryWriter writer)
    {
        writer.WriteUInt8(DescriptorType);
        switch (DescriptorType)
        {
            case 1:
                writer.WriteInt16(NetworkId, true);
                if (NetworkId != 0)
                {
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
