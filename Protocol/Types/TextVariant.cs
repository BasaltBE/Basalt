using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class TextVariant : DataType<TextVariantType>
{
    public TextType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<string> Parameters { get; set; } = [];

    public void Read(ref BinaryReader reader)
    {
        Read(ref reader, TextVariantType.MessageOnly);
    }

    public void Write(ref BinaryWriter writer)
    {
        Write(ref writer, TextVariantType.MessageOnly);
    }

    public void Read(ref BinaryReader reader, TextVariantType parameter)
    {
        Type = (TextType)reader.ReadUInt8();

        switch (parameter)
        {
            case TextVariantType.MessageOnly:
                Message = reader.ReadVarString();
                Source = string.Empty;
                Parameters = [];
                break;
            case TextVariantType.AuthoredMessage:
                Source = reader.ReadVarString();
                Message = reader.ReadVarString();
                Parameters = [];
                break;
            case TextVariantType.MessageWithParameters:
                Message = reader.ReadVarString();
                int count = reader.ReadVarInt();
                Parameters = new List<string>(Math.Max(count, 0));
                for (int i = 0; i < count; i++)
                {
                    Parameters.Add(reader.ReadVarString());
                }

                Source = string.Empty;
                break;
            default:
                throw new InvalidOperationException($"Unsupported text variant type {parameter}.");
        }
    }

    public void Write(ref BinaryWriter writer, TextVariantType parameter)
    {
        writer.WriteUInt8((byte)Type);

        switch (parameter)
        {
            case TextVariantType.MessageOnly:
                writer.WriteVarString(Message);
                break;
            case TextVariantType.AuthoredMessage:
                writer.WriteVarString(Source);
                writer.WriteVarString(Message);
                break;
            case TextVariantType.MessageWithParameters:
                writer.WriteVarString(Message);
                writer.WriteVarInt(Parameters.Count);
                for (int i = 0; i < Parameters.Count; i++)
                {
                    writer.WriteVarString(Parameters[i]);
                }

                break;
            default:
                throw new InvalidOperationException($"Unsupported text variant type {parameter}.");
        }
    }
}
