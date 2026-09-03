using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(312)]
public sealed class ServerboundLoadingScreenPacket : DataPacket {
    public int Type;
    public uint? LoadingScreenId;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt(Type);
        writer.WriteBool(LoadingScreenId.HasValue);
        if (LoadingScreenId is uint loadingScreenId) writer.WriteUInt32(loadingScreenId, true);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Type = reader.ReadVarInt();
        LoadingScreenId = reader.ReadBool() ? reader.ReadUInt32(true) : null;
    }
}
