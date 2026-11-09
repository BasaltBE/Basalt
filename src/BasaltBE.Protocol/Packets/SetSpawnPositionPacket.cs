using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(43)]
public sealed class SetSpawnPositionPacket : DataPacket {
    public SpawnPositionType SpawnType;
    public BlockPos Position = new();
    public int DimensionId;
    public BlockPos CausingBlockPosition = new() { X = int.MinValue, Y = int.MinValue, Z = int.MinValue };

    public override void Deserialize(ref BinaryReader reader) {
        SpawnType = (SpawnPositionType)reader.ReadZigZag();
        Position.Read(ref reader);
        DimensionId = reader.ReadZigZag();
        CausingBlockPosition.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag((int)SpawnType);
        Position.Write(ref writer);
        writer.WriteZigZag(DimensionId);
        CausingBlockPosition.Write(ref writer);
    }
}
