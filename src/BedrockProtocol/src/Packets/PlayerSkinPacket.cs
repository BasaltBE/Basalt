using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(93)]
public sealed class PlayerSkinPacket : DataPacket {
    public Uuid Uuid = new();
    public SerializedSkin SerializedSkin = new();
    public string LocalizedNewSkinName = string.Empty;
    public string LocalizedOldSkinName = string.Empty;

    public override void Serialize(ref BinaryWriter writer) {
        Uuid.Write(ref writer);
        SerializedSkin.Write(ref writer);
        writer.WriteVarString(LocalizedNewSkinName);
        writer.WriteVarString(LocalizedOldSkinName);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Uuid.Read(ref reader);
        SerializedSkin.Read(ref reader);
        LocalizedNewSkinName = reader.ReadVarString();
        LocalizedOldSkinName = reader.ReadVarString();
    }
}
