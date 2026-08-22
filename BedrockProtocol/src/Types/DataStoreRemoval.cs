using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class DataStoreRemoval : DataType {
    public string DataStoreName = string.Empty;

    public override void Write(ref BinaryWriter writer) => writer.WriteVarString(DataStoreName);
    public override void Read(ref BinaryReader reader) => DataStoreName = reader.ReadVarString();
}
