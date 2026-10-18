using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PlayerBlockActionData : DataType {
    public PlayerActionType PlayerActionType;
    public BlockPos Position = new();
    public int Facing;
    public override void Read(ref BinaryReader reader) {
        PlayerActionType = (PlayerActionType)reader.ReadZigZag();
        Position.Read(ref reader);
        Facing = reader.ReadZigZag();
    }
    public override void Write(ref BinaryWriter writer) {
        writer.WriteZigZag((int)PlayerActionType);
        Position.Write(ref writer);
        writer.WriteZigZag(Facing);
    }
}
