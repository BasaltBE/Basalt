using System;
using System.Collections.Generic;
using BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SerializedSkin {
    public string ID = string.Empty;
    public string PlayFabID = string.Empty;
    public string ResourcePatch = string.Empty;
    public SkinImage ImageData = new();
    public List<AnimatedImageData> AnimatedImageData = [];
    public SkinImage CapeImageData = new();
    public string GeometryData = string.Empty;
    public string GeometryDataMinEngineVersion = string.Empty;
    public string AnimationData = string.Empty;
    public string CapeID = string.Empty;
    public string FullID = string.Empty;
    public personaArmSizeType ArmSize = new();
    public Color SkinColor = new();
    public List<SerializedPersonaPieceHandle> PersonaPieces = [];
    public Dictionary<string, TintMapColor> PieceTintColors = [];
    public bool IsPremium;
    public bool IsPersona;
    public bool IsPersonaCapeOnClassicSkin;
    public bool IsPrimaryUser;
    public bool OverridesPlayerAppearance;
    public TrustedSkinFlag TrustedSkinFlag;
    public string ProfileHash = string.Empty;

    public void Read(BinaryReader reader) {
        ID = reader.ReadVarString();
        PlayFabID = reader.ReadVarString();
        ResourcePatch = reader.ReadVarString();
        ImageData.Read(reader);
        int count8 = checked((int)reader.ReadVarUInt());
        AnimatedImageData = new List<AnimatedImageData>(count8);
        for (int i8 = 0; i8 < count8; i8++) {
            AnimatedImageData item8 = default!;
            AnimatedImageData readValue1008 = new();
            readValue1008.Read(reader);
            item8 = readValue1008;
            AnimatedImageData.Add(item8);
        }
        CapeImageData.Read(reader);
        GeometryData = reader.ReadVarString();
        GeometryDataMinEngineVersion = reader.ReadVarString();
        AnimationData = reader.ReadVarString();
        CapeID = reader.ReadVarString();
        FullID = reader.ReadVarString();
        ArmSize = (personaArmSizeType)reader.ReadUInt8();
        SkinColor.Read(reader);
        int count26 = checked((int)reader.ReadVarUInt());
        PersonaPieces = new List<SerializedPersonaPieceHandle>(count26);
        for (int i26 = 0; i26 < count26; i26++) {
            SerializedPersonaPieceHandle item26 = default!;
            SerializedPersonaPieceHandle readValue1026 = new();
            readValue1026.Read(reader);
            item26 = readValue1026;
            PersonaPieces.Add(item26);
        }
        int count28 = checked((int)reader.ReadVarUInt());
        PieceTintColors = new Dictionary<string, TintMapColor>(count28);
        for (int i28 = 0; i28 < count28; i28++) {
            string key28 = default!;
            key28 = reader.ReadVarString();
            TintMapColor value28 = default!;
            TintMapColor readValue2028 = new();
            readValue2028.Read(reader);
            value28 = readValue2028;
            PieceTintColors.Add(key28, value28);
        }
        IsPremium = reader.ReadBool();
        IsPersona = reader.ReadBool();
        IsPersonaCapeOnClassicSkin = reader.ReadBool();
        IsPrimaryUser = reader.ReadBool();
        OverridesPlayerAppearance = reader.ReadBool();
        string enumText40 = reader.ReadVarString();
        TrustedSkinFlag = string.Equals(enumText40, "true", StringComparison.OrdinalIgnoreCase) ? global::BedrockProtocol.Enums.TrustedSkinFlag.True : global::BedrockProtocol.Enums.TrustedSkinFlag.False;
        ProfileHash = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(ID);
        writer.WriteVarString(PlayFabID);
        writer.WriteVarString(ResourcePatch);
        ImageData.Write(writer);
        writer.WriteVarUInt(checked((uint)AnimatedImageData.Count));
        foreach (var item9 in AnimatedImageData) {
            item9.Write(writer);
        }
        CapeImageData.Write(writer);
        writer.WriteVarString(GeometryData);
        writer.WriteVarString(GeometryDataMinEngineVersion);
        writer.WriteVarString(AnimationData);
        writer.WriteVarString(CapeID);
        writer.WriteVarString(FullID);
        writer.WriteUInt8((byte)ArmSize);
        SkinColor.Write(writer);
        writer.WriteVarUInt(checked((uint)PersonaPieces.Count));
        foreach (var item27 in PersonaPieces) {
            item27.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)PieceTintColors.Count));
        foreach (var pair29 in PieceTintColors) {
            writer.WriteVarString(pair29.Key);
            pair29.Value.Write(writer);
        }
        writer.WriteBool(IsPremium);
        writer.WriteBool(IsPersona);
        writer.WriteBool(IsPersonaCapeOnClassicSkin);
        writer.WriteBool(IsPrimaryUser);
        writer.WriteBool(OverridesPlayerAppearance);
        writer.WriteVarString(TrustedSkinFlag == global::BedrockProtocol.Enums.TrustedSkinFlag.True ? "true" : "false");
        writer.WriteVarString(ProfileHash);
    }
}
