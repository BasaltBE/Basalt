using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PresenceConfiguration : DataType {
    public string? RichPresenceId;

    public override void Read(ref BinaryReader reader) => RichPresenceId = reader.ReadBool() ? reader.ReadVarString() : null;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteBool(RichPresenceId is not null);
        if (RichPresenceId is not null)
            writer.WriteVarString(RichPresenceId);
    }
}
