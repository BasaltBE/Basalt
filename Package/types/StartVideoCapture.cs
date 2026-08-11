using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class StartVideoCapture : PlayerVideoCaptureStartVideoCaptureVariant, PlayerVideoCaptureStopVideoCaptureVariant {
    public uint FrameRate;
    public string FilePrefix = string.Empty;

    public void Read(BinaryReader reader) {
        FrameRate = reader.ReadUInt32(true);
        FilePrefix = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(FrameRate, true);
        writer.WriteVarString(FilePrefix);
    }
}
