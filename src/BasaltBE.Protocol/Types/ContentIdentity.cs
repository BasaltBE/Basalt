using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class ContentIdentity : DataType {
    public string Identity = string.Empty;

    public override void Read(ref BinaryReader reader) => Identity = reader.ReadVarString();
    public override void Write(ref BinaryWriter writer) => writer.WriteVarString(Identity);
}
