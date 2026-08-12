#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SetPitch : ClientboundUpdateSoundDataFadeVariant, ClientboundUpdateSoundDataPauseVariant, ClientboundUpdateSoundDataResumeVariant, ClientboundUpdateSoundDataSeekToVariant, ClientboundUpdateSoundDataSetPitchVariant, ClientboundUpdateSoundDataSetVolumeVariant, ClientboundUpdateSoundDataStopVariant {
    public float Pitch;

    public void Read(BinaryReader reader) {
        Pitch = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Pitch, true);
    }
}
