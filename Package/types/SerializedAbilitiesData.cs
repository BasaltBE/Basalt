using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializedAbilitiesData {
    public long TargetPlayerRawId;
    public PlayerPermissionLevel PlayerPermissions;
    public CommandPermissionLevel CommandPermissions;
    public List<SerializedAbilitiesDataSerializedLayer> Layers = [];

    public void Read(BinaryReader reader) {
        TargetPlayerRawId = reader.ReadInt64(true);
        PlayerPermissions = (global::BedrockProtocol.Enums.PlayerPermissionLevel)reader.ReadInt8();
        CommandPermissions = (global::BedrockProtocol.Enums.CommandPermissionLevel)reader.ReadUInt8();
        int count6 = checked((int)reader.ReadVarUInt());
        Layers = new List<SerializedAbilitiesDataSerializedLayer>(count6);
        for (int i6 = 0; i6 < count6; i6++) {
            SerializedAbilitiesDataSerializedLayer item6 = default!;
            SerializedAbilitiesDataSerializedLayer readValue1006 = new();
            readValue1006.Read(reader);
            item6 = readValue1006;
            Layers.Add(item6);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteInt64(TargetPlayerRawId, true);
        writer.WriteInt8((sbyte)PlayerPermissions);
        writer.WriteUInt8((byte)CommandPermissions);
        writer.WriteVarUInt(checked((uint)Layers.Count));
        foreach (var item7 in Layers) {
            item7.Write(writer);
        }
    }
}
