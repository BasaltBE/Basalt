using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SerializedAbilitiesData : DataType {
    public long TargetPlayerRawId;
    public sbyte PlayerPermissions;
    public byte CommandPermissions;
    public SerializedAbilitiesLayer[] Layers = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteInt64(TargetPlayerRawId, true);
        writer.WriteInt8(PlayerPermissions);
        writer.WriteUInt8(CommandPermissions);
        writer.WriteVarUInt((uint)Layers.Length);
        for (int i = 0; i < Layers.Length; i++) Layers[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        TargetPlayerRawId = reader.ReadInt64(true);
        PlayerPermissions = reader.ReadInt8();
        CommandPermissions = reader.ReadUInt8();
        int count = checked((int)reader.ReadVarUInt());
        Layers = new SerializedAbilitiesLayer[count];
        for (int i = 0; i < count; i++) {
            Layers[i] = new SerializedAbilitiesLayer();
            Layers[i].Read(ref reader);
        }
    }
}
