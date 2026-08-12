#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SetVolume : ClientboundUpdateSoundDataFadeVariant, ClientboundUpdateSoundDataPauseVariant, ClientboundUpdateSoundDataResumeVariant, ClientboundUpdateSoundDataSeekToVariant, ClientboundUpdateSoundDataSetPitchVariant, ClientboundUpdateSoundDataSetVolumeVariant, ClientboundUpdateSoundDataStopVariant {
    public float Volume;

    public void Read(BinaryReader reader) {
        Volume = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteF32(Volume, true);
    }
}
