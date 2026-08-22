using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(59)]
public sealed class SetCommandsEnabledPacket : DataPacket {
    public bool Enabled;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(Enabled);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Enabled = reader.ReadBool();
    }
}
