using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class UpdateSubChunkNetworkBlockInfo {
    public BlockPos Pos = new();
    public uint RuntimeId;
    public uint UpdateFlags;
    public ulong SyncMessageEntityUniqueID;
    public uint SyncMessageMessage;

    public void Read(BinaryReader reader) {
        Pos.Read(reader);
        RuntimeId = reader.ReadVarUInt();
        UpdateFlags = reader.ReadVarUInt();
        SyncMessageEntityUniqueID = reader.ReadVarULong();
        SyncMessageMessage = reader.ReadVarUInt();
    }

    public void Write(BinaryWriter writer) {
        Pos.Write(writer);
        writer.WriteVarUInt(RuntimeId);
        writer.WriteVarUInt(UpdateFlags);
        writer.WriteVarULong(SyncMessageEntityUniqueID);
        writer.WriteVarUInt(SyncMessageMessage);
    }
}
