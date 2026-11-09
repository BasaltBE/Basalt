using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PackInfoData : DataType {
    public PackIdVersion IdVersion = new();
    public ulong Size;
    public string ContentKey = string.Empty;
    public string SubpackName = string.Empty;
    public ContentIdentity ContentIdentity = new();
    public bool HasScripts;
    public bool IsAddonPack;
    public bool IsRayTracingCapable;
    public string CdnUrl = string.Empty;

    public override void Read(ref BinaryReader reader) {
        IdVersion.Read(ref reader);
        Size = reader.ReadUInt64(true);
        ContentKey = reader.ReadVarString();
        SubpackName = reader.ReadVarString();
        ContentIdentity.Read(ref reader);
        HasScripts = reader.ReadBool();
        IsAddonPack = reader.ReadBool();
        IsRayTracingCapable = reader.ReadBool();
        CdnUrl = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer) {
        IdVersion.Write(ref writer);
        writer.WriteUInt64(Size, true);
        writer.WriteVarString(ContentKey);
        writer.WriteVarString(SubpackName);
        ContentIdentity.Write(ref writer);
        writer.WriteBool(HasScripts);
        writer.WriteBool(IsAddonPack);
        writer.WriteBool(IsRayTracingCapable);
        writer.WriteVarString(CdnUrl);
    }
}
