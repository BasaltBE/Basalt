using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(308)]
public sealed class SetHudPacket : DataPacket {
    public HudElement[] Elements = [];
    public HudVisibility Visibility;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Elements.Length);
        for (int i = 0; i < Elements.Length; i++) writer.WriteVarInt((int)Elements[i]);
        writer.WriteVarInt((int)Visibility);
    }

    public override void Deserialize(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Elements = new HudElement[count];
        for (int i = 0; i < count; i++) Elements[i] = (HudElement)reader.ReadVarInt();
        Visibility = (HudVisibility)reader.ReadVarInt();
    }
}
