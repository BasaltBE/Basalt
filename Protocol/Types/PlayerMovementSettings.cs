using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PlayerMovementSettings : DataType
{
    /// <summary>
    /// The size of the rewind history
    /// </summary>
    public int RewindHistorySize;

    /// <summary>
    /// Whether the server is authoritative for block breaking or not
    /// </summary>
    public bool ServerAuthoritativeBlockBreaking;

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

