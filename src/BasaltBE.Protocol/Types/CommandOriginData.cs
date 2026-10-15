using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CommandOriginData : DataType {
    public string Type = string.Empty;
    public Uuid Uuid = new();
    public string RequestId = string.Empty;
    public long PlayerId;

    public override void Read(ref BinaryReader reader) {
        Type = reader.ReadVarString();
        Uuid.Read(ref reader);
        RequestId = reader.ReadVarString();
        PlayerId = reader.ReadInt64(true);
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Type);
        Uuid.Write(ref writer);
        writer.WriteVarString(RequestId);
        writer.WriteInt64(PlayerId, true);
    }
}
