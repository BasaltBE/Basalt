using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PlayerMovementSettings : DataType
{
    public int RewindHistorySize { get; set; }
    public bool ServerAuthoritativeBlockBreaking { get; set; }

    public void Read(BinaryReader reader)
    {
        RewindHistorySize = reader.ReadZigZag();
        ServerAuthoritativeBlockBreaking = reader.ReadBool();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteZigZag(RewindHistorySize);
        writer.WriteBool(ServerAuthoritativeBlockBreaking);
    }
}

