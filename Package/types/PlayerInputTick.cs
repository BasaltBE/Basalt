using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerInputTick {
    public ulong InputTick;

    public void Read(BinaryReader reader) {
        InputTick = reader.ReadVarULong();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarULong(InputTick);
    }
}
