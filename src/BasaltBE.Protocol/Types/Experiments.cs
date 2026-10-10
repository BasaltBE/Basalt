using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class Experiments : DataType {
    public ExperimentToggle[] Toggles = [];
    public bool ExperimentsEverToggled;

    public override void Read(ref BinaryReader reader) {
        uint count = reader.ReadUInt32(true);
        Toggles = new ExperimentToggle[(int)count];
        for (int i = 0; i < Toggles.Length; i++) {
            ExperimentToggle toggle = new();
            toggle.Read(ref reader);
            Toggles[i] = toggle;
        }
        ExperimentsEverToggled = reader.ReadBool();
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt32((uint)Toggles.Length, true);
        foreach (ExperimentToggle toggle in Toggles)
            toggle.Write(ref writer);
        writer.WriteBool(ExperimentsEverToggled);
    }
}
