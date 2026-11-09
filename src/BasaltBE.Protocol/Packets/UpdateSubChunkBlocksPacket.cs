using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(172)]
public sealed class UpdateSubChunkBlocksPacket : DataPacket {
    public BlockPos Position = new();
    public UpdateSubChunkBlocksChangedInfo BlocksChanged = new();

    public override void Deserialize(ref BinaryReader reader) {
        Position.Read(ref reader);
        BlocksChanged.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(Position);
        ArgumentNullException.ThrowIfNull(BlocksChanged);
        Position.Write(ref writer);
        BlocksChanged.Write(ref writer);
    }
}
