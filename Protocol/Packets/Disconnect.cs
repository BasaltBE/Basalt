using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Disconnect)]
public sealed record DisconnectPacket : DataPacket
{
    /// <summary>
    /// Disconnect reason code.
    /// </summary>
    public DisconnectReason Reason = DisconnectReason.Unknown;

    /// <summary>
    /// Whether the disconnect screen should be hidden.
    /// </summary>
    public bool HideDisconnectionScreen;

    /// <summary>
    /// Disconnect message text.
    /// </summary>
    public string Message = string.Empty;

    /// <summary>
    /// Filtered message text.
    /// </summary>
    public string FilteredMessage = string.Empty;

    public override void Deserialize(Binary.BinaryReader reader)
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

    public override void Serialize(Binary.BinaryWriter writer)
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
