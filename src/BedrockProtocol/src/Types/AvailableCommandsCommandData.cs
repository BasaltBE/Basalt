using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AvailableCommandsCommandData : DataType {
    public string Name = string.Empty;
    public string Description = string.Empty;
    public ushort Flags;
    public string PermissionLevel = string.Empty;
    public int AliasEnum;
    public uint[] ChainedSubcommandIndexes = [];
    public AvailableCommandsOverloadData[] Overloads = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Name);
        writer.WriteVarString(Description);
        writer.WriteUInt16(Flags, true);
        writer.WriteVarString(PermissionLevel);
        writer.WriteInt32(AliasEnum, true);
        writer.WriteVarUInt((uint)ChainedSubcommandIndexes.Length);
        for (int i = 0; i < ChainedSubcommandIndexes.Length; i++) writer.WriteUInt32(ChainedSubcommandIndexes[i], true);
        writer.WriteVarUInt((uint)Overloads.Length);
        for (int i = 0; i < Overloads.Length; i++) Overloads[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Name = reader.ReadVarString();
        Description = reader.ReadVarString();
        Flags = reader.ReadUInt16(true);
        PermissionLevel = reader.ReadVarString();
        AliasEnum = reader.ReadInt32(true);
        int chainedCount = checked((int)reader.ReadVarUInt());
        ChainedSubcommandIndexes = new uint[chainedCount];
        for (int i = 0; i < chainedCount; i++) ChainedSubcommandIndexes[i] = reader.ReadUInt32(true);
        int overloadCount = checked((int)reader.ReadVarUInt());
        Overloads = new AvailableCommandsOverloadData[overloadCount];
        for (int i = 0; i < overloadCount; i++) {
            Overloads[i] = new AvailableCommandsOverloadData();
            Overloads[i].Read(ref reader);
        }
    }
}
