using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CommandOutputMessage : DataType {
    public string Id = string.Empty;
    public bool Successful;
    public string[] Parameters = [];

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadVarString();
        Successful = reader.ReadBool();
        Parameters = new string[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Parameters.Length; index++)
            Parameters[index] = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Id);
        writer.WriteBool(Successful);
        writer.WriteVarUInt((uint)Parameters.Length);
        foreach (string parameter in Parameters)
            writer.WriteVarString(parameter);
    }
}
