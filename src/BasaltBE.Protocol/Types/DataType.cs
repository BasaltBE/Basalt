using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public abstract class DataType {
    public abstract void Read(ref BinaryReader reader);
    public abstract void Write(ref BinaryWriter writer);
}
