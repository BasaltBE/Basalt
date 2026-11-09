using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PropertySyncIntEntry : DataType {
    public uint PropertyIndex;
    public int Data;

    public override void Read(ref BinaryReader reader) {
        PropertyIndex = reader.ReadVarUInt();
        Data = checked((int)reader.ReadZigZong());
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt(PropertyIndex);
        writer.WriteZigZong(Data);
    }
}
