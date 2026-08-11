using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketChainedSubcommandData {
    public string Name = string.Empty;
    public List<AvailableCommandsPacketChainedSubcommandRelationship>? SubCommandValues;

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        if (reader.ReadBool()) {
            int count2 = checked((int)reader.ReadVarUInt());
            SubCommandValues = new List<AvailableCommandsPacketChainedSubcommandRelationship>(count2);
            for (int i2 = 0; i2 < count2; i2++) {
                AvailableCommandsPacketChainedSubcommandRelationship item2 = default!;
                AvailableCommandsPacketChainedSubcommandRelationship readValue1002 = new();
                readValue1002.Read(reader);
                item2 = readValue1002;
                SubCommandValues.Add(item2);
            }
        } else {
            SubCommandValues = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteBool(SubCommandValues is not null);
        if (SubCommandValues is { } optionalValue3) {
            writer.WriteVarUInt(checked((uint)optionalValue3.Count));
            foreach (var item3 in optionalValue3) {
                item3.Write(writer);
            }
        }
    }
}
