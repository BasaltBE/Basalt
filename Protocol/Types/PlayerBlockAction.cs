using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PlayerBlockAction : DataType
{
    public PlayerActionType Action { get; set; }
    public BlockPos BlockPos { get; set; }
    public int Face { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Action = (PlayerActionType)reader.ReadZigZag();
        BlockPos pos = BlockPos;
        pos.Read(ref reader);
        BlockPos = pos;
        Face = reader.ReadZigZag();
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag((int)Action);
        BlockPos.Write(ref writer);
        writer.WriteZigZag(Face);
    }
}
