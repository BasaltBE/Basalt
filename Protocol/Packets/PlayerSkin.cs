using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.PlayerSkin)]
public sealed record PlayerSkinPacket : DataPacket {
    /// <summary>The player's UUID.</summary>
    public Guid UUID;

    /// <summary>The player's serialized new skin.</summary>
    public SerializedSkin Skin = new();

    /// <summary>The player's localized new skin name.</summary>
    public string LocalizedNewSkinName = string.Empty;

    /// <summary>The player's localized old skin name.</summary>
    public string LocalizedOldSkinName = string.Empty;

    /// <summary>Whether the skin is verified.</summary>
    public bool Verified = true;

    public override void Deserialize(Binary.BinaryReader reader) {
        UUID = Types.UUID.Read(reader);
        Skin.Read(reader);
        LocalizedNewSkinName = reader.ReadVarString();
        LocalizedOldSkinName = reader.ReadVarString();
        Verified = reader.ReadBool();
    }

    public override void Serialize(Binary.BinaryWriter writer) {
        Types.UUID.Write(writer, UUID);
        Skin.Write(writer);
        writer.WriteVarString(LocalizedNewSkinName);
        writer.WriteVarString(LocalizedOldSkinName);
        writer.WriteBool(Verified);
    }
}
