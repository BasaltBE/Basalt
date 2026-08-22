using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class PlayerListAddEntry : DataType {
    public Uuid Uuid = new();
    public long ActorUniqueId;
    public string PlayerName = string.Empty;
    public string Xuid = string.Empty;
    public string PlatformOnlineId = string.Empty;
    public int BuildPlatform;
    public SerializedSkin Skin = new();
    public bool Teacher;
    public bool Host;
    public bool SubClient;
    public int PlayerColor;

    public override void Write(ref BinaryWriter writer) {
        Uuid.Write(ref writer);
        writer.WriteVarLong(ActorUniqueId);
        writer.WriteVarString(PlayerName);
        writer.WriteVarString(Xuid);
        writer.WriteVarString(PlatformOnlineId);
        writer.WriteInt32(BuildPlatform, true);
        Skin.Write(ref writer);
        writer.WriteBool(Teacher);
        writer.WriteBool(Host);
        writer.WriteBool(SubClient);
        writer.WriteInt32(PlayerColor, true);
    }

    public override void Read(ref BinaryReader reader) {
        Uuid.Read(ref reader);
        ActorUniqueId = reader.ReadVarLong();
        PlayerName = reader.ReadVarString();
        Xuid = reader.ReadVarString();
        PlatformOnlineId = reader.ReadVarString();
        BuildPlatform = reader.ReadInt32(true);
        Skin.Read(ref reader);
        Teacher = reader.ReadBool();
        Host = reader.ReadBool();
        SubClient = reader.ReadBool();
        PlayerColor = reader.ReadInt32(true);
    }
}
