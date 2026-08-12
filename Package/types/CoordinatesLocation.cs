#nullable enable

using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CoordinatesLocation : PlayerLocationVariant {
    public PlayerLocationPacketType PacketType;
    public Vec3 Position = new();

    public void Read(BinaryReader reader) {
        PacketType = (global::BedrockProtocol.Enums.PlayerLocationPacketType)reader.ReadZigZag();
        Position.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)PacketType);
        Position.Write(writer);
    }
}
