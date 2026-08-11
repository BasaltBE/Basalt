using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerBlockActionData {
    public PlayerActionType PlayerActionType;
    public BlockPos Position = new();
    public int Facing;

    public void Read(BinaryReader reader) {
        PlayerActionType = (global::BedrockProtocol.Enums.PlayerActionType)reader.ReadZigZag();
        Position.Read(reader);
        Facing = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag((int)PlayerActionType);
        Position.Write(writer);
        writer.WriteZigZag(Facing);
    }
}
