using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ClientPixelsProxy {
    public uint Pixel;
    public ushort Index;

    public void Read(BinaryReader reader) {
        Pixel = reader.ReadUInt32(true);
        Index = reader.ReadUInt16(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(Pixel, true);
        writer.WriteUInt16(Index, true);
    }
}
