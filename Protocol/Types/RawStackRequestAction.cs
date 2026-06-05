using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class RawStackRequestAction : IStackRequestAction, DataType
{
    /// <summary>
    /// Raw stack action type id.
    /// </summary>
    public byte Type;
    /// <summary>
    /// Raw action payload bytes.
    /// </summary>
    public byte[] Data = [];
    public byte ActionType => Type;
    public void Read(BinaryReader reader) { }
    public void Write(BinaryWriter writer) => writer.WriteBytes(Data);
}
