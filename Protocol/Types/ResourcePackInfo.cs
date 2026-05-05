using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ResourcePackInfo
{
    public Guid Uuid { get; set; } = Guid.Empty;
    public string Version { get; set; } = "1.0.0";
    public ulong Size { get; set; }
    public string ContentKey { get; set; } = string.Empty;
    public string SubPackName { get; set; } = string.Empty;
    public string ContentIdentity { get; set; } = string.Empty;
    public bool HasScripts { get; set; }
    public bool HasAddons { get; set; }
    public bool RtxEnabled { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;

    public void Read(ref BinaryReader reader)
    {
        Span<byte> uuidBytes = stackalloc byte[16];
        reader.ReadBytes(16).CopyTo(uuidBytes);
        uuidBytes[..8].Reverse();
        uuidBytes[8..].Reverse();
        Uuid = new Guid(uuidBytes);
        Version = reader.ReadVarString();
        Size = reader.ReadUInt64(true);
        ContentKey = reader.ReadVarString();
        SubPackName = reader.ReadVarString();
        ContentIdentity = reader.ReadVarString();
        HasScripts = reader.ReadBool();
        HasAddons = reader.ReadBool();
        RtxEnabled = reader.ReadBool();
        DownloadUrl = reader.ReadVarString();
    }

    public void Write(ref BinaryWriter writer)
    {
        byte[] uuidBytes = Uuid.ToByteArray();
        uuidBytes[..8].Reverse();
        uuidBytes[8..].Reverse();
        writer.WriteBytes(uuidBytes);
        writer.WriteVarString(Version);
        writer.WriteUInt64(Size, true);
        writer.WriteVarString(ContentKey);
        writer.WriteVarString(SubPackName);
        writer.WriteVarString(ContentIdentity);
        writer.WriteBool(HasScripts);
        writer.WriteBool(HasAddons);
        writer.WriteBool(RtxEnabled);
        writer.WriteVarString(DownloadUrl);
    }

    public void Deserialize(ref BinaryReader reader) => Read(ref reader);
    public void Serialize(ref BinaryWriter writer) => Write(ref writer);
}
