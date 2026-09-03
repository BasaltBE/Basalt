using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(140)]
public sealed class SettingsCommandPacket : DataPacket {
    public string Command = string.Empty;
    public bool SuppressOutput;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(Command);
        writer.WriteBool(SuppressOutput);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Command = reader.ReadVarString();
        SuppressOutput = reader.ReadBool();
    }
}
