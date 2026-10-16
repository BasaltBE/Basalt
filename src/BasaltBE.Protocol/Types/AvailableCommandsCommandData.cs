using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class AvailableCommandsCommandData : DataType
{
    public string Name = string.Empty;
    public string Description = string.Empty;
    public ushort Flags;
    public string PermissionLevel = "any";
    public int AliasEnum = -1;
    public uint[] ChainedSubcommandIndexes = [];
    public AvailableCommandsOverloadData[] Overloads = [];

    public override void Read(ref BinaryReader reader)
    {
        Name = reader.ReadVarString();
        Description = reader.ReadVarString();
        Flags = reader.ReadUInt16(true);
        PermissionLevel = reader.ReadVarString();
        AliasEnum = reader.ReadInt32(true);
        ChainedSubcommandIndexes = new uint[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < ChainedSubcommandIndexes.Length; index++)
            ChainedSubcommandIndexes[index] = reader.ReadUInt32(true);
        Overloads = new AvailableCommandsOverloadData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < Overloads.Length; index++)
        {
            AvailableCommandsOverloadData overload = new();
            overload.Read(ref reader);
            Overloads[index] = overload;
        }
    }

    public override void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Name);
        writer.WriteVarString(Description);
        writer.WriteUInt16(Flags, true);
        writer.WriteVarString(PermissionLevel);
        writer.WriteInt32(AliasEnum, true);
        writer.WriteVarUInt((uint)ChainedSubcommandIndexes.Length);
        foreach (uint index in ChainedSubcommandIndexes)
            writer.WriteUInt32(index, true);
        writer.WriteVarUInt((uint)Overloads.Length);
        foreach (AvailableCommandsOverloadData overload in Overloads)
            overload.Write(ref writer);
    }
}
