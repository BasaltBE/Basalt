using Basalt.Binary;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(9)]
public sealed class TextPacket : DataPacket {
    public bool Localize;
    public TextPacketBody Body = new();
    public string SenderXuid = string.Empty;
    public string PlatformId = string.Empty;
    public string? FilteredMessage;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(Localize);
        Body.Write(ref writer);
        writer.WriteVarString(SenderXuid);
        writer.WriteVarString(PlatformId);
        writer.WriteBool(FilteredMessage is not null);
        if (FilteredMessage is not null) writer.WriteVarString(FilteredMessage);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Localize = reader.ReadBool();
        Body.Read(ref reader);
        SenderXuid = reader.ReadVarString();
        PlatformId = reader.ReadVarString();
        FilteredMessage = reader.ReadBool() ? reader.ReadVarString() : null;
    }
}
