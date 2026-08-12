#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Fade : ClientboundUpdateSoundDataFadeVariant, ClientboundUpdateSoundDataPauseVariant, ClientboundUpdateSoundDataResumeVariant, ClientboundUpdateSoundDataSeekToVariant, ClientboundUpdateSoundDataSetPitchVariant, ClientboundUpdateSoundDataSetVolumeVariant, ClientboundUpdateSoundDataStopVariant {
    public float Duration;
    public float TargetVolume;

    public void Read(BinaryReader reader) {
        Duration = reader.ReadF32(true);
        TargetVolume = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Duration, true);
        writer.WriteF32(TargetVolume, true);
    }
}
