using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(93)]
public sealed class PlayerSkinPacket : DataPacket {
    public Uuid Uuid = new();
    public SerializedSkin Skin = new();
    public string NewSkinName = string.Empty;
    public string OldSkinName = string.Empty;

    public override void Deserialize(ref BinaryReader reader) {
        Uuid.Read(ref reader);
        Skin.Read(ref reader);
        NewSkinName = reader.ReadVarString();
        OldSkinName = reader.ReadVarString();
    }

    public override void Serialize(ref BinaryWriter writer) {
        Uuid.Write(ref writer);
        Skin.Write(ref writer);
        writer.WriteVarString(NewSkinName);
        writer.WriteVarString(OldSkinName);
    }
}
