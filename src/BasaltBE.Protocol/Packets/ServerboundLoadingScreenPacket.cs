using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(312)]
public sealed class ServerboundLoadingScreenPacket : DataPacket {
    public LoadingScreenPacketType Type;
    public uint? LoadingScreenId;

    public override void Deserialize(ref BinaryReader reader) {
        Type = (LoadingScreenPacketType)reader.ReadZigZag();
        LoadingScreenId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag((int)Type);
        writer.WriteBool(LoadingScreenId is not null);
        if (LoadingScreenId is uint loadingScreenId)
            writer.WriteUInt32(loadingScreenId, true);
    }
}
