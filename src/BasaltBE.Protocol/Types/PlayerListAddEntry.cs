using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class PlayerListAddEntry : DataType {
    public Uuid Uuid = new();
    public long ActorId;
    public string Name = string.Empty;
    public string Xuid = string.Empty;
    public string PlayFabId = string.Empty;
    public string PlatformOnlineId = string.Empty;
    public BuildPlatform BuildPlatform;
    public SerializedSkin Skin = new();
    public bool IsTeacher;
    public bool IsHost;
    public bool IsSubClient;
    public int PlayerColor;

    public override void Read(ref BinaryReader reader) {
        Uuid.Read(ref reader);
        ActorId = reader.ReadZigZong();
        Name = reader.ReadVarString();
        Xuid = reader.ReadVarString();
        PlayFabId = reader.ReadVarString();
        PlatformOnlineId = reader.ReadVarString();
        BuildPlatform = (BuildPlatform)reader.ReadInt32(true);
        Skin.Read(ref reader);
        IsTeacher = reader.ReadBool();
        IsHost = reader.ReadBool();
        IsSubClient = reader.ReadBool();
        PlayerColor = reader.ReadInt32(true);
    }

    public override void Write(ref BinaryWriter writer) {
        Uuid.Write(ref writer);
        writer.WriteZigZong(ActorId);
        writer.WriteVarString(Name);
        writer.WriteVarString(Xuid);
        writer.WriteVarString(PlayFabId);
        writer.WriteVarString(PlatformOnlineId);
        writer.WriteInt32((int)BuildPlatform, true);
        Skin.Write(ref writer);
        writer.WriteBool(IsTeacher);
        writer.WriteBool(IsHost);
        writer.WriteBool(IsSubClient);
        writer.WriteInt32(PlayerColor, true);
    }
}
