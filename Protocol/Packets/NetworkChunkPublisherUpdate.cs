using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record NetworkChunkPublisherUpdatePacket : DataPacket
{
    public int CoordinateX { get; set; }
    public int CoordinateY { get; set; }
    public int CoordinateZ { get; set; }
    public uint Radius { get; set; }
    public List<(int X, int Z)> SavedChunks { get; set; } = [];

    public override PacketId PacketId => PacketId.NetworkChunkPublisherUpdate;

    public override void Deserialize(ref BinaryReader reader)
    {
        CoordinateX = reader.ReadZigZag();
        CoordinateY = unchecked((int)reader.ReadVarUInt());
        CoordinateZ = reader.ReadZigZag();
        Radius = reader.ReadVarUInt();

        int savedChunkCount = reader.ReadInt32(true);
        if (savedChunkCount < 0)
        {
            savedChunkCount = 0;
        }

        SavedChunks = new List<(int X, int Z)>(savedChunkCount);
        for (int i = 0; i < savedChunkCount; i++)
        {
            int x = reader.ReadZigZag();
            int z = reader.ReadZigZag();
            SavedChunks.Add((x, z));
        }
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteZigZag(CoordinateX);
        writer.WriteVarUInt(unchecked((uint)CoordinateY));
        writer.WriteZigZag(CoordinateZ);
        writer.WriteVarUInt(Radius);

        writer.WriteInt32(SavedChunks.Count, true);
        for (int i = 0; i < SavedChunks.Count; i++)
        {
            (int x, int z) = SavedChunks[i];
            writer.WriteZigZag(x);
            writer.WriteZigZag(z);
        }
    }
}
