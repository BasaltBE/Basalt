using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PackInfoData : DataType {
    public PackIdVersion PackIdVersion = new();
    public ulong PackSize;
    public string ContentKey = string.Empty;
    public string SubpackName = string.Empty;
    public string ContentIdentity = string.Empty;
    public bool HasScripts;
    public bool IsAddonPack;
    public bool IsRayTracingCapable;
    public string CdnUrl = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        PackIdVersion.Write(ref writer);
        writer.WriteUInt64(PackSize, true);
        writer.WriteVarString(ContentKey);
        writer.WriteVarString(SubpackName);
        writer.WriteVarString(ContentIdentity);
        writer.WriteBool(HasScripts);
        writer.WriteBool(IsAddonPack);
        writer.WriteBool(IsRayTracingCapable);
        writer.WriteVarString(CdnUrl);
    }

    public override void Read(ref BinaryReader reader) {
        PackIdVersion.Read(ref reader);
        PackSize = reader.ReadUInt64(true);
        ContentKey = reader.ReadVarString();
        SubpackName = reader.ReadVarString();
        ContentIdentity = reader.ReadVarString();
        HasScripts = reader.ReadBool();
        IsAddonPack = reader.ReadBool();
        IsRayTracingCapable = reader.ReadBool();
        CdnUrl = reader.ReadVarString();
    }
}
