using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerListRemoveEntry : PlayerListEntryVariant {
    public PlayerListPacketType Action;
    public UUID UUID = new();

    public void Read(BinaryReader reader) {
        Action = (global::BedrockProtocol.Enums.PlayerListPacketType)reader.ReadUInt8();
        UUID.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)Action);
        UUID.Write(writer);
    }
}
