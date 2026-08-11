using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PackInfoData {
    public PackIdVersion PackIdVersion = new();
    public ulong PackSize;
    public string ContentKey = string.Empty;
    public string SubpackName = string.Empty;
    public ContentIdentity ContentIdentity = new();
    public bool HasScripts;
    public bool IsAddonPack;
    public bool IsRayTracingCapable;
    public string CDNURL = string.Empty;

    public void Read(BinaryReader reader) {
        PackIdVersion.Read(reader);
        PackSize = reader.ReadUInt64(true);
        ContentKey = reader.ReadVarString();
        SubpackName = reader.ReadVarString();
        ContentIdentity.Read(reader);
        HasScripts = reader.ReadBool();
        IsAddonPack = reader.ReadBool();
        IsRayTracingCapable = reader.ReadBool();
        CDNURL = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        PackIdVersion.Write(writer);
        writer.WriteUInt64(PackSize, true);
        writer.WriteVarString(ContentKey);
        writer.WriteVarString(SubpackName);
        ContentIdentity.Write(writer);
        writer.WriteBool(HasScripts);
        writer.WriteBool(IsAddonPack);
        writer.WriteBool(IsRayTracingCapable);
        writer.WriteVarString(CDNURL);
    }
}
