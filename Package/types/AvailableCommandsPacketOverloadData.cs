#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketOverloadData {
    public bool IsChaining;
    public List<AvailableCommandsPacketParamData> ParameterData = [];

    public void Read(BinaryReader reader) {
        IsChaining = reader.ReadBool();
        int count2 = checked((int)reader.ReadVarUInt());
        ParameterData = new List<AvailableCommandsPacketParamData>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            AvailableCommandsPacketParamData item2 = default!;
            AvailableCommandsPacketParamData readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            ParameterData.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(IsChaining);
        writer.WriteVarUInt(checked((uint)ParameterData.Count));
        foreach (var item3 in ParameterData) {
            item3.Write(writer);
        }
    }
}
