using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SerializedSkin : DataType
{
    public string Id = string.Empty;
    public string ResourcePatch = string.Empty;
    public SkinImage ImageData = new();
    public AnimatedImageData[] AnimatedImageData = [];
    public SkinImage CapeImageData = new();
    public string GeometryData = string.Empty;
    public string GeometryDataMinEngineVersion = string.Empty;
    public string AnimationData = string.Empty;
    public string CapeId = string.Empty;
    public string FullId = string.Empty;
    public PersonaArmSize ArmSize;
    public int SkinColor;
    public SerializedPersonaPieceHandle[] PersonaPieces = [];
    public Dictionary<string, TintMapColor> PieceTintColors = [];
    public bool IsPremium;
    public bool IsPersona;
    public bool IsPersonaCapeOnClassicSkin;
    public bool IsPrimaryUser;
    public bool OverridesPlayerAppearance;
    public TrustedSkinFlag TrustedSkinFlag;
    public string ProfileHash = string.Empty;

    public override void Read(ref BinaryReader reader)
    {
        Id = reader.ReadVarString();
        ResourcePatch = reader.ReadVarString();
        ImageData.Read(ref reader);
        AnimatedImageData = new AnimatedImageData[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < AnimatedImageData.Length; i++)
        {
            AnimatedImageData[i] = new AnimatedImageData();
            AnimatedImageData[i].Read(ref reader);
        }
        CapeImageData.Read(ref reader);
        GeometryData = reader.ReadVarString();
        GeometryDataMinEngineVersion = reader.ReadVarString();
        AnimationData = reader.ReadVarString();
        CapeId = reader.ReadVarString();
        FullId = reader.ReadVarString();
        ArmSize = (PersonaArmSize)reader.ReadUInt8();
        SkinColor = reader.ReadInt32(true);
        PersonaPieces = new SerializedPersonaPieceHandle[checked((int)reader.ReadVarUInt())];
        for (int i = 0; i < PersonaPieces.Length; i++)
        {
            PersonaPieces[i] = new SerializedPersonaPieceHandle();
            PersonaPieces[i].Read(ref reader);
        }
        PieceTintColors = new Dictionary<string, TintMapColor>();
        uint tintCount = reader.ReadVarUInt();
        for (int i = 0; i < tintCount; i++)
        {
            string pieceType = reader.ReadVarString();
            TintMapColor colors = new();
            colors.Read(ref reader);
            PieceTintColors.Add(pieceType, colors);
        }
        IsPremium = reader.ReadBool();
        IsPersona = reader.ReadBool();
        IsPersonaCapeOnClassicSkin = reader.ReadBool();
        IsPrimaryUser = reader.ReadBool();
        OverridesPlayerAppearance = reader.ReadBool();
        TrustedSkinFlag = Enum.Parse<TrustedSkinFlag>(reader.ReadVarString(), ignoreCase: true);
        ProfileHash = reader.ReadVarString();
    }

    public override void Write(ref BinaryWriter writer)
    {
        writer.WriteVarString(Id);
        writer.WriteVarString(ResourcePatch);
        ImageData.Write(ref writer);
        writer.WriteVarUInt((uint)AnimatedImageData.Length);
        foreach (AnimatedImageData image in AnimatedImageData)
            image.Write(ref writer);
        CapeImageData.Write(ref writer);
        writer.WriteVarString(GeometryData);
        writer.WriteVarString(GeometryDataMinEngineVersion);
        writer.WriteVarString(AnimationData);
        writer.WriteVarString(CapeId);
        writer.WriteVarString(FullId);
        writer.WriteUInt8((byte)ArmSize);
        writer.WriteInt32(SkinColor, true);
        writer.WriteVarUInt((uint)PersonaPieces.Length);
        foreach (SerializedPersonaPieceHandle piece in PersonaPieces)
            piece.Write(ref writer);
        writer.WriteVarUInt((uint)PieceTintColors.Count);
        foreach ((string pieceType, TintMapColor colors) in PieceTintColors)
        {
            writer.WriteVarString(pieceType);
            colors.Write(ref writer);
        }
        writer.WriteBool(IsPremium);
        writer.WriteBool(IsPersona);
        writer.WriteBool(IsPersonaCapeOnClassicSkin);
        writer.WriteBool(IsPrimaryUser);
        writer.WriteBool(OverridesPlayerAppearance);
        writer.WriteVarString(TrustedSkinFlag switch
        {
            TrustedSkinFlag.Unset => "Unset",
            TrustedSkinFlag.False => "False",
            TrustedSkinFlag.True => "True",
            _ => throw new InvalidDataException("Unknown trusted skin flag.")
        });
        writer.WriteVarString(ProfileHash);
    }
}
