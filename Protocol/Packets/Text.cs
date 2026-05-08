using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record TextPacket : DataPacket
{
    public bool NeedsTranslation { get; set; }
    public TextVariantType VariantType { get; set; } = TextVariantType.MessageOnly;
    public TextVariant Variant { get; set; } = new();
    public string Xuid { get; set; } = string.Empty;
    public string PlatformChatId { get; set; } = string.Empty;
    public string? FilteredMessage { get; set; }

    public override PacketId PacketId => PacketId.Text;

    public override void Deserialize(ref BinaryReader reader)
    {
        NeedsTranslation = reader.ReadBool();
        VariantType = (TextVariantType)reader.ReadUInt8();
        Variant = new TextVariant();
        Variant.Read(ref reader, VariantType);
        Xuid = reader.ReadVarString();
        PlatformChatId = reader.ReadVarString();
        bool hasFilteredMessage = reader.ReadBool();
        FilteredMessage = hasFilteredMessage ? reader.ReadVarString() : null;
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteBool(NeedsTranslation);
        writer.WriteUInt8((byte)VariantType);
        Variant.Write(ref writer, VariantType);
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
