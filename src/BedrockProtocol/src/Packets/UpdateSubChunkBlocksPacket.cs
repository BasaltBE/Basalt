using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(172)]
public sealed class UpdateSubChunkBlocksPacket : DataPacket {
    public BlockPos SubChunkBlockPosition = new();
    public UpdateSubChunkBlocksChangedInfo BlocksChanged = new();

    public override void Serialize(ref BinaryWriter writer) {
        SubChunkBlockPosition.Write(ref writer);
        BlocksChanged.Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        SubChunkBlockPosition.Read(ref reader);
        BlocksChanged.Read(ref reader);
    }
}
