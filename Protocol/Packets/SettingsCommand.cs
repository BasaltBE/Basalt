using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SettingsCommand)]
public sealed record SettingsCommandPacket : DataPacket {
    public string CommandLine = string.Empty;
    public bool SuppressOutput;

    public override void Deserialize(Binary.BinaryReader reader) {
        CommandLine = reader.ReadVarString();
        SuppressOutput = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarString(CommandLine);
        writer.WriteBool(SuppressOutput);
    }
}
