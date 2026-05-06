using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public interface DataType
{
    void Read(ref BinaryReader reader);
    void Write(ref BinaryWriter writer);
}

