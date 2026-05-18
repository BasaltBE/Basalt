using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public interface DataType
{
    void Read(BinaryReader reader);
    void Write(BinaryWriter writer);
}

public interface DataType<TParameter> : DataType
{
    void Read(BinaryReader reader, TParameter parameter);
    void Write(BinaryWriter writer, TParameter parameter);
}

