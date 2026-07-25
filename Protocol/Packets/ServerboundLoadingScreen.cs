using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ServerboundLoadingScreen)]
public sealed record ServerboundLoadingScreenPacket : DataPacket {
    public LoadingScreenType Type;
    public uint? LoadingScreenId;

    public override void Deserialize(Binary.BinaryReader reader) {
        Type = (LoadingScreenType)reader.ReadVarInt();
        LoadingScreenId = reader.ReadBool()
            ? reader.ReadUInt32(true)
            : null;
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarInt((int)Type);
        writer.WriteBool(LoadingScreenId.HasValue);
        if (LoadingScreenId.HasValue) {
            writer.WriteUInt32(LoadingScreenId.Value, true);
        }
    }
}
