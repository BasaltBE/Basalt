using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class CommandOutputMessage : DataType {
    public string MessageId = string.Empty;
    public bool Successful;
    public string[] Parameters = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(MessageId);
        writer.WriteBool(Successful);
        writer.WriteVarUInt((uint)Parameters.Length);
        for (int i = 0; i < Parameters.Length; i++) writer.WriteVarString(Parameters[i]);
    }

    public override void Read(ref BinaryReader reader) {
        MessageId = reader.ReadVarString();
        Successful = reader.ReadBool();
        int count = checked((int)reader.ReadVarUInt());
        Parameters = new string[count];
        for (int i = 0; i < count; i++) Parameters[i] = reader.ReadVarString();
    }
}
