using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BlockCommandData : CommandBlockUpdateTargetVariant {
    public BlockPos BlockPosition = new();
    public uint CommandBlockMode;
    public bool RedstoneMode;
    public bool IsConditional;

    public void Read(BinaryReader reader) {
        BlockPosition.Read(reader);
        CommandBlockMode = reader.ReadVarUInt();
        RedstoneMode = reader.ReadBool();
        IsConditional = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        BlockPosition.Write(writer);
        writer.WriteVarUInt(CommandBlockMode);
        writer.WriteBool(RedstoneMode);
        writer.WriteBool(IsConditional);
    }
}
