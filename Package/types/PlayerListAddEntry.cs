using System;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PlayerListAddEntry : PlayerListEntryVariant {
    public PlayerListPacketType Action;
    public UUID UUID = new();
    public ActorUniqueID ActorUniqueID = new();
    public string PlayerName = string.Empty;
    public string XBLXUID = string.Empty;
    public string PlatformOnlineID = string.Empty;
    public BuildPlatform BuildPlatform;
    public SerializedSkin SerializedSkin = new();
    public bool IsTeacher;
    public bool IsHost;
    public bool IsSubClient;
    public Color PlayerColor = new();

    public void Read(BinaryReader reader) {
        Action = (global::BedrockProtocol.Enums.PlayerListPacketType)reader.ReadUInt8();
        UUID.Read(reader);
        ActorUniqueID.Read(reader);
        PlayerName = reader.ReadVarString();
        XBLXUID = reader.ReadVarString();
        PlatformOnlineID = reader.ReadVarString();
        BuildPlatform = (global::BedrockProtocol.Enums.BuildPlatform)reader.ReadInt32(true);
        SerializedSkin.Read(reader);
        IsTeacher = reader.ReadBool();
        IsHost = reader.ReadBool();
        IsSubClient = reader.ReadBool();
        PlayerColor.Read(reader);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt8((byte)Action);
        UUID.Write(writer);
        ActorUniqueID.Write(writer);
        writer.WriteVarString(PlayerName);
        writer.WriteVarString(XBLXUID);
        writer.WriteVarString(PlatformOnlineID);
        writer.WriteInt32((int)BuildPlatform, true);
        SerializedSkin.Write(writer);
        writer.WriteBool(IsTeacher);
        writer.WriteBool(IsHost);
        writer.WriteBool(IsSubClient);
        PlayerColor.Write(writer);
    }
}
