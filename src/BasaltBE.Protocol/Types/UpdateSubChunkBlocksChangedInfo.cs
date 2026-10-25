using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class UpdateSubChunkBlocksChangedInfo : DataType {
    public UpdateSubChunkNetworkBlockInfo[] Standards = [];
    public UpdateSubChunkNetworkBlockInfo[] Extras = [];

    public override void Read(ref BinaryReader reader) {
        uint standardCount = reader.ReadVarUInt();
        if (standardCount > 4096 || standardCount > reader.Remaining / 7)
            throw new InvalidDataException("Standard subchunk block update count is invalid.");

        Standards = new UpdateSubChunkNetworkBlockInfo[(int)standardCount];
        for (int i = 0; i < Standards.Length; i++) {
            UpdateSubChunkNetworkBlockInfo block = new();
            block.Read(ref reader);
            Standards[i] = block;
        }

        uint extraCount = reader.ReadVarUInt();
        if (extraCount > 4096 || extraCount > reader.Remaining / 7)
            throw new InvalidDataException("Extra subchunk block update count is invalid.");

        Extras = new UpdateSubChunkNetworkBlockInfo[(int)extraCount];
        for (int i = 0; i < Extras.Length; i++) {
            UpdateSubChunkNetworkBlockInfo block = new();
            block.Read(ref reader);
            Extras[i] = block;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        if (Standards.Length > 4096 || Extras.Length > 4096)
            throw new InvalidDataException("Subchunk block update count cannot exceed 4096 entries per layer.");
        writer.WriteVarUInt((uint)Standards.Length);
        foreach (UpdateSubChunkNetworkBlockInfo block in Standards) {
            ArgumentNullException.ThrowIfNull(block);
            block.Write(ref writer);
        }
        writer.WriteVarUInt((uint)Extras.Length);
        foreach (UpdateSubChunkNetworkBlockInfo block in Extras) {
            ArgumentNullException.ThrowIfNull(block);
            block.Write(ref writer);
        }
    }
}
