using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class UpdateSubChunkBlocksChangedInfo : DataType {
    public UpdateSubChunkNetworkBlockInfo[] Standards = [];
    public UpdateSubChunkNetworkBlockInfo[] Extras = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Standards.Length);
        for (int i = 0; i < Standards.Length; i++) Standards[i].Write(ref writer);
        writer.WriteVarUInt((uint)Extras.Length);
        for (int i = 0; i < Extras.Length; i++) Extras[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Standards = ReadBlocks(ref reader);
        Extras = ReadBlocks(ref reader);
    }

    static UpdateSubChunkNetworkBlockInfo[] ReadBlocks(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        UpdateSubChunkNetworkBlockInfo[] blocks = new UpdateSubChunkNetworkBlockInfo[count];
        for (int i = 0; i < count; i++) {
            blocks[i] = new UpdateSubChunkNetworkBlockInfo();
            blocks[i].Read(ref reader);
        }
        return blocks;
    }
}
