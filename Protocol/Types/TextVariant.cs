using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class TextVariant
{
    /// <summary>
    /// Text display type (chat, tip, popup, etc).
    /// </summary>
    public TextType Type;

    /// <summary>
    /// Main message body.
    /// </summary>
    public string Message = string.Empty;

    /// <summary>
    /// Message source/sender when authored.
    /// </summary>
    public string Source = string.Empty;

    /// <summary>
    /// Translation parameters for parameterized messages.
    /// </summary>
    public List<string> Parameters = [];

    public void Read(BinaryReader reader, int parameter = 0)
    {
        TextVariantType variantType = (TextVariantType)parameter;
        Type = (TextType)reader.ReadUInt8();

        switch (variantType)
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
                throw new InvalidOperationException($"Unsupported text variant type {variantType}.");
        }
    }

    public void Write(BinaryWriter writer, int parameter = 0)
    {
        TextVariantType variantType = (TextVariantType)parameter;
        writer.WriteUInt8((byte)Type);

        switch (variantType)
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
                throw new InvalidOperationException($"Unsupported text variant type {variantType}.");
        }
    }
}
