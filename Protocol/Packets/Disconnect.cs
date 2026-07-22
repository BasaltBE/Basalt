using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.Disconnect)]
public sealed record DisconnectPacket : DataPacket {
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

    public override void Deserialize(Binary.BinaryReader reader) {
        Reason = (DisconnectReason)reader.ReadVarUInt();
        HideDisconnectionScreen = reader.ReadBool();

        if (!HideDisconnectionScreen) {
            Message = reader.ReadVarString();
            if (reader.Remaining > 0) {
                bool hasFilteredMessage = reader.ReadBool();
                FilteredMessage = hasFilteredMessage ? reader.ReadVarString() : Message;
            }
            else {
                FilteredMessage = Message;
            }
        }
        else {
            Message = string.Empty;
            FilteredMessage = string.Empty;
        }
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarUInt((uint)Reason);
        writer.WriteBool(HideDisconnectionScreen);

        if (!HideDisconnectionScreen) {
            writer.WriteVarString(Message);
            bool hasFilteredMessage = !string.IsNullOrEmpty(FilteredMessage) &&
                                      !string.Equals(FilteredMessage, Message, StringComparison.Ordinal);
            writer.WriteBool(hasFilteredMessage);
            if (hasFilteredMessage) {
                writer.WriteVarString(FilteredMessage);
            }
        }
    }
}
