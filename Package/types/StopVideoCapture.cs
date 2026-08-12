#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class StopVideoCapture : PlayerVideoCaptureStartVideoCaptureVariant, PlayerVideoCaptureStopVideoCaptureVariant {
    public void Read(BinaryReader reader) {
    }

    public void Write(BinaryWriter writer) {
    }
}
