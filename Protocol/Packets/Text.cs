using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Text)]
public sealed record TextPacket : DataPacket
{
    /// <summary>
    /// Whether the client should treat this as a translatable message.
    /// </summary>
    public bool NeedsTranslation;

    /// <summary>
    /// Variant payload layout that follows.
    /// </summary>
    public TextVariantType VariantType = TextVariantType.MessageOnly;

    /// <summary>
    /// The text payload for the selected variant.
    /// </summary>
    public TextVariant Variant = new();

    /// <summary>
    /// Sender XUID, often empty for server/system messages.
    /// </summary>
    public string Xuid = string.Empty;

    /// <summary>
    /// Platform chat id, usually empty unless a platform integration uses it.
    /// </summary>
    public string PlatformChatId = string.Empty;

    /// <summary>
    /// Optional filtered message override.
    /// </summary>
    public string? FilteredMessage;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        NeedsTranslation = reader.ReadBool();
        VariantType = (TextVariantType)reader.ReadUInt8();
        Variant = new TextVariant();
        Variant.Read(reader, (int)VariantType);
        Xuid = reader.ReadVarString();
        PlatformChatId = reader.ReadVarString();
        bool hasFilteredMessage = reader.ReadBool();
        FilteredMessage = hasFilteredMessage ? reader.ReadVarString() : null;
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteBool(NeedsTranslation);
        writer.WriteUInt8((byte)VariantType);
        Variant.Write(writer, (int)VariantType);
        writer.WriteVarString(Xuid);
        writer.WriteVarString(PlatformChatId);
        bool hasFilteredMessage = FilteredMessage is not null;
        writer.WriteBool(hasFilteredMessage);
        if (hasFilteredMessage)
        {
            writer.WriteVarString(FilteredMessage!);
        }
    }
}
