#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Stop : ClientboundUpdateSoundDataFadeVariant, ClientboundUpdateSoundDataPauseVariant, ClientboundUpdateSoundDataResumeVariant, ClientboundUpdateSoundDataSeekToVariant, ClientboundUpdateSoundDataSetPitchVariant, ClientboundUpdateSoundDataSetVolumeVariant, ClientboundUpdateSoundDataStopVariant {
    #pragma warning disable CA1822

    public void Read(BinaryReader reader) {
    }

    public void Write(BinaryWriter writer) {
    }

    #pragma warning restore CA1822
}
