using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class CommandOutput : DataType {
    public CommandOutputType OutputType;
    public uint SuccessCount;
    public CommandOutputMessage[] Messages = [];
    public string? DataSet;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(OutputTypeToString(OutputType));
        writer.WriteUInt32(SuccessCount, true);
        writer.WriteVarUInt((uint)Messages.Length);
        for (int i = 0; i < Messages.Length; i++) Messages[i].Write(ref writer);
        writer.WriteBool(DataSet is not null);
        if (DataSet is string dataSet) writer.WriteVarString(dataSet);
    }

    public override void Read(ref BinaryReader reader) {
        OutputType = StringToOutputType(reader.ReadVarString());
        SuccessCount = reader.ReadUInt32(true);
        int count = checked((int)reader.ReadVarUInt());
        Messages = new CommandOutputMessage[count];
        for (int i = 0; i < count; i++) {
            Messages[i] = new CommandOutputMessage();
            Messages[i].Read(ref reader);
        }
        DataSet = reader.ReadBool() ? reader.ReadVarString() : null;
    }

    private static string OutputTypeToString(CommandOutputType outputType) => outputType switch {
        CommandOutputType.None => "none",
        CommandOutputType.LastOutput => "lastoutput",
        CommandOutputType.Silent => "silent",
        CommandOutputType.AllOutput => "alloutput",
        CommandOutputType.DataSet => "dataset",
        _ => throw new InvalidOperationException($"Unknown command output type: {outputType}.")
    };

    private static CommandOutputType StringToOutputType(string outputType) => outputType switch {
        "none" => CommandOutputType.None,
        "lastoutput" => CommandOutputType.LastOutput,
        "silent" => CommandOutputType.Silent,
        "alloutput" => CommandOutputType.AllOutput,
        "dataset" => CommandOutputType.DataSet,
        _ => throw new InvalidOperationException($"Unknown command output type: {outputType}.")
    };
}
