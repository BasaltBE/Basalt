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
        PlayerActionType = (PlayerActionType)reader.ReadVarInt();
        Position.Read(ref reader);
        Facing = reader.ReadVarInt();
    }
    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarInt((int)PlayerActionType);
        Position.Write(ref writer);
        writer.WriteVarInt(Facing);
    }
}
