using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record DisconnectPacket : DataPacket
{
    public DisconnectReason Reason { get; set; } = DisconnectReason.Unknown;
    public bool HideDisconnectionScreen { get; set; }
    public string Message { get; set; } = string.Empty;
    public string FilteredMessage { get; set; } = string.Empty;

    public override PacketId PacketId => PacketId.Disconnect;

    public override void Deserialize(ref BinaryReader reader)
    {
        Reason = (DisconnectReason)reader.ReadVarInt();
        HideDisconnectionScreen = reader.ReadBool();

        if (!HideDisconnectionScreen)
        {
            Message = reader.ReadVarString();
            FilteredMessage = reader.ReadVarString();
        }
        else
        {
            Message = string.Empty;
            FilteredMessage = string.Empty;
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarInt((int)Reason);
        writer.WriteBool(HideDisconnectionScreen);

        if (!HideDisconnectionScreen)
        {
            writer.WriteVarString(Message);
            writer.WriteVarString(FilteredMessage);
        }
    }
}
