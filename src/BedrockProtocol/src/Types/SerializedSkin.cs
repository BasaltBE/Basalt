using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class SerializedSkin : DataType {
    public string Id = string.Empty;
    public string PlayFabId = string.Empty;
    public string ResourcePatch = string.Empty;
    public SkinImage ImageData = new();
    public AnimatedImageData[] AnimatedImageData = [];
    public SkinImage CapeImageData = new();
    public string GeometryData = string.Empty;
    public string GeometryDataMinEngineVersion = string.Empty;
    public string AnimationData = string.Empty;
    public string CapeId = string.Empty;
    public string FullId = string.Empty;
    public ArmSizeType ArmSize;
    public int SkinColor;
    public SerializedPersonaPieceHandle[] PersonaPieces = [];
    public Dictionary<string, TintMapColor> PieceTintColors = new(StringComparer.Ordinal);
    public bool Premium;
    public bool Persona;
    public bool PersonaCapeOnClassicSkin;
    public bool PrimaryUser;
    public bool OverridesPlayerAppearance;
    public TrustedSkinFlag TrustedSkinFlag;
    public string ProfileHash = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(Id);
        writer.WriteVarString(PlayFabId);
        writer.WriteVarString(ResourcePatch);
        ImageData.Write(ref writer);
        writer.WriteVarUInt((uint)AnimatedImageData.Length);
        foreach (AnimatedImageData image in AnimatedImageData) image.Write(ref writer);
        CapeImageData.Write(ref writer);
        writer.WriteVarString(GeometryData);
        writer.WriteVarString(GeometryDataMinEngineVersion);
        writer.WriteVarString(AnimationData);
        writer.WriteVarString(CapeId);
        writer.WriteVarString(FullId);
        writer.WriteUInt8((byte)ArmSize);
        writer.WriteInt32(SkinColor, true);
        writer.WriteVarUInt((uint)PersonaPieces.Length);
        foreach (SerializedPersonaPieceHandle piece in PersonaPieces) piece.Write(ref writer);
        writer.WriteVarUInt((uint)PieceTintColors.Count);
        foreach ((string key, TintMapColor value) in PieceTintColors) {
            writer.WriteVarString(key);
            value.Write(ref writer);
        }
        writer.WriteBool(Premium);
        writer.WriteBool(Persona);
        writer.WriteBool(PersonaCapeOnClassicSkin);
        writer.WriteBool(PrimaryUser);
        writer.WriteBool(OverridesPlayerAppearance);
        writer.WriteVarString(TrustedSkinFlag.ToString());
        writer.WriteVarString(ProfileHash);
    }

    public override void Read(ref BinaryReader reader) {
        Id = reader.ReadVarString();
        PlayFabId = reader.ReadVarString();
        ResourcePatch = reader.ReadVarString();
        ImageData.Read(ref reader);
        AnimatedImageData = new AnimatedImageData[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < AnimatedImageData.Length; index++) {
            AnimatedImageData image = new();
            image.Read(ref reader);
            AnimatedImageData[index] = image;
        }
        CapeImageData.Read(ref reader);
        GeometryData = reader.ReadVarString();
        GeometryDataMinEngineVersion = reader.ReadVarString();
        AnimationData = reader.ReadVarString();
        CapeId = reader.ReadVarString();
        FullId = reader.ReadVarString();
        ArmSize = (ArmSizeType)reader.ReadUInt8();
        SkinColor = reader.ReadInt32(true);
        PersonaPieces = new SerializedPersonaPieceHandle[checked((int)reader.ReadVarUInt())];
        for (int index = 0; index < PersonaPieces.Length; index++) {
            SerializedPersonaPieceHandle piece = new();
            piece.Read(ref reader);
            PersonaPieces[index] = piece;
        }
        PieceTintColors = new(StringComparer.Ordinal);
        for (int index = 0; index < reader.ReadVarUInt(); index++) {
            string key = reader.ReadVarString();
            TintMapColor value = new();
            value.Read(ref reader);
            PieceTintColors[key] = value;
        }
        Premium = reader.ReadBool();
        Persona = reader.ReadBool();
        PersonaCapeOnClassicSkin = reader.ReadBool();
        PrimaryUser = reader.ReadBool();
        OverridesPlayerAppearance = reader.ReadBool();
        TrustedSkinFlag = Enum.Parse<TrustedSkinFlag>(reader.ReadVarString(), true);
        ProfileHash = reader.ReadVarString();
    }
}
