using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class CameraPresets_2537458058 {
    public List<CameraPresets> Presets = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Presets = new List<CameraPresets>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            CameraPresets item0 = default!;
            CameraPresets readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Presets.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Presets.Count));
        foreach (var item1 in Presets) {
            item1.Write(writer);
        }
    }
}
