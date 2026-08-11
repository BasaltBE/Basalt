using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AimAssistActorPriorityData {
    public int PresetIndex;
    public int CategoryIndex;
    public int ActorIndex;
    public int PriorityValue;

    public void Read(BinaryReader reader) {
        PresetIndex = reader.ReadInt32(true);
        CategoryIndex = reader.ReadInt32(true);
        ActorIndex = reader.ReadInt32(true);
        PriorityValue = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt32(PresetIndex, true);
        writer.WriteInt32(CategoryIndex, true);
        writer.WriteInt32(ActorIndex, true);
        writer.WriteInt32(PriorityValue, true);
    }
}
