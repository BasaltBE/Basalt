using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsParamData : DataType {
    public string Name = string.Empty;
    public uint ParseSymbol;
    public bool Optional;
    public byte Options;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteUInt32(ParseSymbol, true);
        writer.WriteBool(Optional);
        writer.WriteUInt8(Options);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        ParseSymbol = reader.ReadUInt32(true);
        Optional = reader.ReadBool();
        Options = reader.ReadUInt8();
    }
}
