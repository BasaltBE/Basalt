using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class CommandOutput : DataType {
    public string Type = "None";
    public uint SuccessCount;
    public CommandOutputMessage[] Messages = [];
    public string? DataSet;

    public override void Read(ref BinaryReader reader) {
        Type = reader.ReadVarString();
        SuccessCount = reader.ReadUInt32(true);
        Messages = new CommandOutputMessage[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Messages.Length; index++) {
            CommandOutputMessage message = new();
            message.Read(ref reader);
            Messages[index] = message;
        }
        if (reader.Remaining > 0)
            DataSet = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Type);
        writer.WriteUInt32(SuccessCount, true);
        writer.WriteVarUInt((uint)Messages.Length);
        foreach (CommandOutputMessage message in Messages)
            message.Write(ref writer);
        if (DataSet is not null)
            writer.WriteVarString(DataSet);
    }
}
