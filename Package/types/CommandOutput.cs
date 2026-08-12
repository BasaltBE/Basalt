#nullable enable

using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CommandOutput {
    public global::BedrockProtocol.Enums.CommandOutputType OutputType;
    public uint SuccessCount;
    public List<CommandOutputMessage> OutputMessages = [];
    public string? DataSet;

    public void Read(BinaryReader reader) {
        string enumText0 = reader.ReadVarString();
        OutputType = enumText0 switch {
            "none" => global::BedrockProtocol.Enums.CommandOutputType.None,
            "lastoutput" => global::BedrockProtocol.Enums.CommandOutputType.LastOutput,
            "silent" => global::BedrockProtocol.Enums.CommandOutputType.Silent,
            "alloutput" => global::BedrockProtocol.Enums.CommandOutputType.AllOutput,
            "dataset" => global::BedrockProtocol.Enums.CommandOutputType.DataSet,
            _ => throw new InvalidOperationException($"Unknown CommandOutputType wire value: {enumText0}"),
        };
        SuccessCount = reader.ReadUInt32(true);
        int count4 = checked((int)reader.ReadVarUInt());
        OutputMessages = new List<CommandOutputMessage>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            CommandOutputMessage item4 = default!;
            CommandOutputMessage readValue1004 = new();
            readValue1004.Read(reader);
            item4 = readValue1004;
            OutputMessages.Add(item4);
        }
        if (reader.ReadBool()) {
            DataSet = reader.ReadVarString();
        } else {
            DataSet = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(OutputType switch {
            global::BedrockProtocol.Enums.CommandOutputType.None => "none",
            global::BedrockProtocol.Enums.CommandOutputType.LastOutput => "lastoutput",
            global::BedrockProtocol.Enums.CommandOutputType.Silent => "silent",
            global::BedrockProtocol.Enums.CommandOutputType.AllOutput => "alloutput",
            global::BedrockProtocol.Enums.CommandOutputType.DataSet => "dataset",
            _ => throw new InvalidOperationException($"Unknown CommandOutputType value: {OutputType}"),
        });
        writer.WriteUInt32(SuccessCount, true);
        writer.WriteVarUInt(checked((uint)OutputMessages.Count));
        foreach (var item5 in OutputMessages) {
            item5.Write(writer);
        }
        writer.WriteBool(DataSet is not null);
        if (DataSet is { } optionalValue7) {
            writer.WriteVarString(optionalValue7);
        }
    }
}
