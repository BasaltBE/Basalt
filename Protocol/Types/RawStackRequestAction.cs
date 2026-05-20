using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class RawStackRequestAction : IStackRequestAction, DataType
{
    public byte Type { get; set; }
    public byte[] Data { get; set; } = [];
    public byte ActionType => Type;
    public void Read(BinaryReader reader) { }
    public void Write(BinaryWriter writer) => writer.WriteBytes(Data);
}
