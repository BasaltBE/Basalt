#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class HiddenLocation : PlayerLocationVariant {
    public PlayerLocationPacketType PacketType;

    public void Read(BinaryReader reader) {
        PacketType = (global::BedrockProtocol.Enums.PlayerLocationPacketType)reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)PacketType);
    }
}
