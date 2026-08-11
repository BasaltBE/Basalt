using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class AvailableCommandsPacketParamData {
    public string Name = string.Empty;
    public uint ParseSymbol;
    public bool IsOptional;
    public byte Options;

    public void Read(BinaryReader reader) {
        Name = reader.ReadVarString();
        ParseSymbol = reader.ReadUInt32(true);
        IsOptional = reader.ReadBool();
        Options = reader.ReadUInt8();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteUInt32(ParseSymbol, true);
        writer.WriteBool(IsOptional);
        writer.WriteUInt8(Options);
    }
}
