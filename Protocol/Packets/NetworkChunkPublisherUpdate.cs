using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.NetworkChunkPublisherUpdate)]
public sealed record NetworkChunkPublisherUpdatePacket : DataPacket {
    /// <summary>
    /// Publisher center X in world coordinates.
    /// </summary>
    public int CoordinateX;

    /// <summary>
    /// Publisher center Y in world coordinates.
    /// </summary>
    public int CoordinateY;

    /// <summary>
    /// Publisher center Z in world coordinates.
    /// </summary>
    public int CoordinateZ;

    /// <summary>
    /// Publisher radius in blocks.
    /// </summary>
    public uint Radius;

    /// <summary>
    /// Already-known chunk coordinates
    /// </summary>
    public List<(int X, int Z)> SavedChunks = [];

    public override void Deserialize(Binary.BinaryReader reader) {
        CoordinateX = reader.ReadZigZag();
        CoordinateY = unchecked((int)reader.ReadVarUInt());
        CoordinateZ = reader.ReadZigZag();
        Radius = reader.ReadVarUInt();

        int savedChunkCount = reader.ReadInt32(true);
        if (savedChunkCount < 0) {
            savedChunkCount = 0;
        }

        SavedChunks = new List<(int X, int Z)>(savedChunkCount);
        for (int i = 0; i < savedChunkCount; i++) {
            int x = reader.ReadZigZag();
            int z = reader.ReadZigZag();
            SavedChunks.Add((x, z));
        }
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteZigZag(CoordinateX);
        writer.WriteVarUInt(unchecked((uint)CoordinateY));
        writer.WriteZigZag(CoordinateZ);
        writer.WriteVarUInt(Radius);

        writer.WriteInt32(SavedChunks.Count, true);
        for (int i = 0; i < SavedChunks.Count; i++) {
            (int x, int z) = SavedChunks[i];
            writer.WriteZigZag(x);
            writer.WriteZigZag(z);
        }
    }
}
