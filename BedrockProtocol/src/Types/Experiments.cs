using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class Experiments : DataType {
    public ExperimentToggle[] Toggles = Array.Empty<ExperimentToggle>();
    public bool ExperimentsEverToggled;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt32((uint)Toggles.Length, true);

        foreach (ExperimentToggle toggle in Toggles) {
            toggle.Write(ref writer);
        }

        writer.WriteBool(ExperimentsEverToggled);
    }

    public override void Read(ref BinaryReader reader) {
        int count = checked((int)reader.ReadUInt32(true));
        Toggles = new ExperimentToggle[count];

        for (int index = 0; index < count; index++) {
            ExperimentToggle toggle = new();
            toggle.Read(ref reader);
            Toggles[index] = toggle;
        }

        ExperimentsEverToggled = reader.ReadBool();
    }
}
