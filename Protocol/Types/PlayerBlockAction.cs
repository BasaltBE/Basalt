using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PlayerBlockAction : DataType
{
    public int Action { get; set; }
    public BlockPos BlockPos { get; set; }
    public int Face { get; set; }

    public void Read(ref BinaryReader reader)
    {
        Action = reader.ReadZigZag();
        if (Action is 0 or 1 or 18 or 26 or 27)
        {
            BlockPos.Read(ref reader);
            Face = reader.ReadZigZag();
        }
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteZigZag(Action);
        if (Action is 0 or 1 or 18 or 26 or 27)
        {
            BlockPos.Write(ref writer);
            writer.WriteZigZag(Face);
        }
    }
}
