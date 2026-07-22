using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class PresenceInfo : DataType {
    /// <summary>
    /// Optional experience name.
    /// </summary>
    public OptionalValue<string> ExperienceName = new();

    /// <summary>
    /// Optional world name.
    /// </summary>
    public OptionalValue<string> WorldName = new();

    /// <summary>
    /// Rich presence id overriding the client-provided value.
    /// </summary>
    public string RichPresenceId = string.Empty;

    public void Read(BinaryReader reader) {
        ExperienceName.Read(reader, static r => r.ReadVarString());
        WorldName.Read(reader, static r => r.ReadVarString());
        RichPresenceId = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        ExperienceName.Write(writer, static (w, value) => w.WriteVarString(value));
        WorldName.Write(writer, static (w, value) => w.WriteVarString(value));
        writer.WriteVarString(RichPresenceId);
    }
}


