#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Experiments {
    public List<ExperimentToggle> Toggles = [];
    public bool ExperimentsEverToggled;

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadUInt32(true));
        Toggles = new List<ExperimentToggle>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            ExperimentToggle item0 = default!;
            ExperimentToggle readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Toggles.Add(item0);
        }
        ExperimentsEverToggled = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(checked((uint)Toggles.Count), true);
        foreach (var item1 in Toggles) {
            item1.Write(writer);
        }
        writer.WriteBool(ExperimentsEverToggled);
    }
}
