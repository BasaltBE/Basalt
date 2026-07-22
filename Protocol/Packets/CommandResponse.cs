using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

/// <summary>
/// Sends output for a command request.
/// </summary>
[Packet(PacketId.CommandResponse)]
public sealed record CommandResponsePacket : DataPacket {
    /// <summary>
    /// Origin data matching the command request.
    /// </summary>
    public CommandOrigin Origin = new();

    /// <summary>
    /// Type of command output sent in this response.
    /// </summary>
    public CommandOutputType OutputType = CommandOutputType.AllOutput;

    /// <summary>
    /// Number of successful command executions.
    /// </summary>
    public uint SuccessCount;

    /// <summary>
    /// Output messages returned by the command.
    /// </summary>
    public List<CommandOutputMessage> OutputMessages = [];

    /// <summary>
    /// Optional data set returned with the command output.
    /// </summary>
    public string? DataSet;

    public override void Deserialize(Binary.BinaryReader reader) {
        Origin = new CommandOrigin();
        Origin.Read(reader);
        OutputType = OutputTypeFromString(reader.ReadVarString());
        SuccessCount = reader.ReadUInt32(true);

        int count = checked((int)reader.ReadVarUInt());
        OutputMessages = new(count);
        for (int i = 0; i < count; i++) {
            CommandOutputMessage message = new();
            message.Read(reader);
            OutputMessages.Add(message);
        }

        DataSet = reader.ReadBool() ? reader.ReadVarString() : null;
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        Origin.Write(writer);
        writer.WriteVarString(OutputTypeToString(OutputType));
        writer.WriteUInt32(SuccessCount, true);
        writer.WriteVarUInt((uint)OutputMessages.Count);
        for (int i = 0; i < OutputMessages.Count; i++) {
            OutputMessages[i].Write(writer);
        }

        bool dataSetPresent = DataSet is not null;
        writer.WriteBool(dataSetPresent);
        if (dataSetPresent) {
            writer.WriteVarString(DataSet!);
        }
    }

    static string OutputTypeToString(CommandOutputType outputType) => outputType switch {
        CommandOutputType.None => "none",
        CommandOutputType.LastOutput => "lastoutput",
        CommandOutputType.Silent => "silent",
        CommandOutputType.AllOutput => "alloutput",
        CommandOutputType.DataSet => "dataset",
        _ => "unknown"
    };

    static CommandOutputType OutputTypeFromString(string outputType) => outputType switch {
        "none" => CommandOutputType.None,
        "lastoutput" => CommandOutputType.LastOutput,
        "silent" => CommandOutputType.Silent,
        "alloutput" => CommandOutputType.AllOutput,
        "dataset" => CommandOutputType.DataSet,
        _ => throw new InvalidOperationException($"Unknown command output type: {outputType}.")
    };
}
