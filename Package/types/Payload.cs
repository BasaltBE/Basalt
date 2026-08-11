using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class Payload {
    public uint UpdateFlag;
    public bool? IsVisible;
    public WorldPosition? WorldPosition;
    public string? TexturePath;
    public Vec2? IconSize;
    public Color? Color;
    public bool? ClientPositionAuthority;
    public ActorUniqueID? ActorUniqueID;

    public void Read(BinaryReader reader) {
        UpdateFlag = reader.ReadUInt32(true);
        if (reader.ReadBool()) {
            IsVisible = reader.ReadBool();
        } else {
            IsVisible = default;
        }
        if (reader.ReadBool()) {
            WorldPosition readValue4 = new();
            readValue4.Read(reader);
            WorldPosition = readValue4;
        } else {
            WorldPosition = default;
        }
        if (reader.ReadBool()) {
            TexturePath = reader.ReadVarString();
        } else {
            TexturePath = default;
        }
        if (reader.ReadBool()) {
            Vec2 readValue8 = new();
            readValue8.Read(reader);
            IconSize = readValue8;
        } else {
            IconSize = default;
        }
        if (reader.ReadBool()) {
            Color readValue10 = new();
            readValue10.Read(reader);
            Color = readValue10;
        } else {
            Color = default;
        }
        if (reader.ReadBool()) {
            ClientPositionAuthority = reader.ReadBool();
        } else {
            ClientPositionAuthority = default;
        }
        if (reader.ReadBool()) {
            ActorUniqueID readValue14 = new();
            readValue14.Read(reader);
            ActorUniqueID = readValue14;
        } else {
            ActorUniqueID = default;
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(UpdateFlag, true);
        writer.WriteBool(IsVisible is not null);
        if (IsVisible is { } optionalValue3) {
            writer.WriteBool(optionalValue3);
        }
        writer.WriteBool(WorldPosition is not null);
        if (WorldPosition is { } optionalValue5) {
            optionalValue5.Write(writer);
        }
        writer.WriteBool(TexturePath is not null);
        if (TexturePath is { } optionalValue7) {
            writer.WriteVarString(optionalValue7);
        }
        writer.WriteBool(IconSize is not null);
        if (IconSize is { } optionalValue9) {
            optionalValue9.Write(writer);
        }
        writer.WriteBool(Color is not null);
        if (Color is { } optionalValue11) {
            optionalValue11.Write(writer);
        }
        writer.WriteBool(ClientPositionAuthority is not null);
        if (ClientPositionAuthority is { } optionalValue13) {
            writer.WriteBool(optionalValue13);
        }
        writer.WriteBool(ActorUniqueID is not null);
        if (ActorUniqueID is { } optionalValue15) {
            optionalValue15.Write(writer);
        }
    }
}
