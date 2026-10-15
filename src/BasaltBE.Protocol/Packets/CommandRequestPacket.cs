using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(77)]
public sealed class CommandRequestPacket : DataPacket {
    public string Command = string.Empty;
    public CommandOriginData Origin = new();
    public bool Internal;
    public string Version = "Latest";

    public override void Deserialize(ref BinaryReader reader) {
        Command = reader.ReadVarString();
        Origin.Read(ref reader);
        Internal = reader.ReadBool();
        Version = reader.ReadVarString();
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(Command);
        Origin.Write(ref writer);
        writer.WriteBool(Internal);
        writer.WriteVarString(Version);
    }
}
