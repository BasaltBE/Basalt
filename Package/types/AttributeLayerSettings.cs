using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AttributeLayerSettings {
    public int Priority;
    public float Weight;
    public bool Enabled;
    public bool TransitionsPaused;

    public void Read(BinaryReader reader) {
        Priority = reader.ReadInt32(true);
        Weight = reader.ReadF32(true);
        Enabled = reader.ReadBool();
        TransitionsPaused = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(Priority, true);
        writer.WriteF32(Weight, true);
        writer.WriteBool(Enabled);
        writer.WriteBool(TransitionsPaused);
    }
}
