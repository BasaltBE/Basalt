using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class ResourcePackInfo : DataType {
    /// <summary>
    /// Unique Identifier of a resource pack
    /// </summary>
    public Guid Uuid = Guid.Empty;

    /// <summary>
    /// Version of the resource pack
    /// </summary>
    public string Version = "1.0.0";

    /// <summary>
    /// Size of the resource pack
    /// </summary>
    public ulong Size;

    /// <summary>
    /// Content key of the resource pack
    /// </summary>
    public string ContentKey = string.Empty;

    /// <summary>
    /// Sub pack name of the resource pack
    /// </summary>
    public string SubPackName = string.Empty;

    /// <summary>
    /// Content identity of the resource pack
    /// </summary>
    public string ContentIdentity = string.Empty;

    /// <summary>
    /// Whether the resource pack has scripts or not
    /// </summary>
    public bool HasScripts;

    /// <summary>
    /// Whether the resource pack has addons or not
    /// </summary>
    public bool HasAddons;

    /// <summary>
    /// Whether the resource pack has RTX enabled or not
    /// </summary>
    public bool RtxEnabled;

    /// <summary>
    /// Download URL of the resource pack
    /// </summary>
    public string DownloadUrl = string.Empty;

    public void Read(BinaryReader reader) {
        Uuid = UUID.Read(reader);
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

    public void Write(BinaryWriter writer) {
        UUID.Write(writer, Uuid);
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
}


