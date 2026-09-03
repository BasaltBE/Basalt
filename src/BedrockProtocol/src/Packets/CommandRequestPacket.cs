using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(77)]
public sealed class CommandRequestPacket : DataPacket {
    public string Command = string.Empty;
    public CommandOriginData Origin = new();
    public bool IsInternal;
    public string Version = string.Empty;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(Command);
        Origin.Write(ref writer);
        writer.WriteBool(IsInternal);
        writer.WriteVarString(Version);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Command = reader.ReadVarString();
        Origin.Read(ref reader);
        IsInternal = reader.ReadBool();
        Version = reader.ReadVarString();
    }
}
