using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SerializedAbilitiesData : DataType {
    public long TargetPlayerRawId;
    public PlayerPermissionLevel PlayerPermissions;
    public CommandPermissionLevel CommandPermissions;
    public SerializedAbilitiesLayer[] Layers = [];

    public override void Read(ref BinaryReader reader) {
        TargetPlayerRawId = reader.ReadInt64(true);
        PlayerPermissions = (PlayerPermissionLevel)reader.ReadInt8();
        CommandPermissions = (CommandPermissionLevel)reader.ReadUInt8();
        Layers = new SerializedAbilitiesLayer[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < Layers.Length; i++) {
            SerializedAbilitiesLayer layer = new();
            layer.Read(ref reader);
            Layers[i] = layer;
        }
    }

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt64(TargetPlayerRawId, true);
        writer.WriteInt8((sbyte)PlayerPermissions);
        writer.WriteUInt8((byte)CommandPermissions);
        writer.WriteVarUInt((uint)Layers.Length);
        foreach (SerializedAbilitiesLayer layer in Layers)
            layer.Write(ref writer);
    }
}
