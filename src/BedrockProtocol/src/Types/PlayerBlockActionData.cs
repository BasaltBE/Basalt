using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PlayerBlockActionData : DataType {
    public PlayerActionType ActionType;
    public BlockPos Position = new();
    public int Facing;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag((int)ActionType);
        Position.Write(ref writer);
        writer.WriteZigZag(Facing);
    }

    public override void Read(ref BinaryReader reader) {
        ActionType = (PlayerActionType)reader.ReadZigZag();
        Position.Read(ref reader);
        Facing = reader.ReadZigZag();
    }
}
