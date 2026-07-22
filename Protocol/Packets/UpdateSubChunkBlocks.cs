using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

/// <summary>
/// Batch version of UpdateBlock for updating many blocks within a sub-chunk at once.
/// </summary>
[Packet(PacketId.UpdateSubChunkBlocks)]
public sealed record UpdateSubChunkBlocksPacket : DataPacket {
  /// <summary>
  /// Sub-chunk position (chunk X, sub-chunk Y index, chunk Z).
  /// </summary>
  public int SubChunkX;
  public int SubChunkY;
  public int SubChunkZ;

  /// <summary>
  /// Block changes for the primary layer.
  /// </summary>
  public List<BlockChangeEntry> Blocks = [];

  /// <summary>
  /// Block changes for the secondary layer (waterlogging).
  /// </summary>
  public List<BlockChangeEntry> Extra = [];

  public override void Deserialize(Binary.BinaryReader reader) {
    SubChunkX = reader.ReadZigZag();
    SubChunkY = reader.ReadZigZag();
    SubChunkZ = reader.ReadZigZag();

    uint blocksLen = reader.ReadVarUInt();
    Blocks = new List<BlockChangeEntry>((int)blocksLen);
    for (uint i = 0; i < blocksLen; i++) {
      Blocks.Add(BlockChangeEntry.Read(reader));
    }

    uint extraLen = reader.ReadVarUInt();
    Extra = new List<BlockChangeEntry>((int)extraLen);
    for (uint i = 0; i < extraLen; i++) {
      Extra.Add(BlockChangeEntry.Read(reader));
    }
  }

  public override void Serialize(Binary.BinaryWriter writer) {
    writer.WriteZigZag(SubChunkX);
    writer.WriteZigZag(SubChunkY);
    writer.WriteZigZag(SubChunkZ);

    writer.WriteVarUInt((uint)Blocks.Count);
    for (int i = 0; i < Blocks.Count; i++) {
      Blocks[i].Write(writer);
    }

    writer.WriteVarUInt((uint)Extra.Count);
    for (int i = 0; i < Extra.Count; i++) {
      Extra[i].Write(writer);
    }
  }
}
