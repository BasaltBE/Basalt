using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class Skin : DataType
{
    /// <summary>
    /// Skin id value.
    /// </summary>
    public string SkinId = string.Empty;

    /// <summary>
    /// PlayFab id value.
    /// </summary>
    public string PlayFabId = string.Empty;

    /// <summary>
    /// Skin resource patch bytes.
    /// </summary>
    public byte[] SkinResourcePatch = [];

    /// <summary>
    /// Skin image width.
    /// </summary>
    public uint SkinImageWidth;

    /// <summary>
    /// Skin image height.
    /// </summary>
    public uint SkinImageHeight;

    /// <summary>
    /// Raw skin image bytes.
    /// </summary>
    public byte[] SkinData = [];

    /// <summary>
    /// Skin animations.
    /// </summary>
    public List<SkinAnimation> Animations = [];

    /// <summary>
    /// Cape image width.
    /// </summary>
    public uint CapeImageWidth;

    /// <summary>
    /// Cape image height.
    /// </summary>
    public uint CapeImageHeight;

    /// <summary>
    /// Raw cape image bytes.
    /// </summary>
    public byte[] CapeData = [];

    /// <summary>
    /// Skin geometry bytes.
    /// </summary>
    public byte[] SkinGeometry = [];

    /// <summary>
    /// Geometry engine version bytes.
    /// </summary>
    public byte[] GeometryDataEngineVersion = [];

    /// <summary>
    /// Skin animation data bytes.
    /// </summary>
    public byte[] AnimationData = [];

    /// <summary>
    /// Cape id value.
    /// </summary>
    public string CapeId = string.Empty;

    /// <summary>
    /// Full skin id value.
    /// </summary>
    public string FullId = string.Empty;

    /// <summary>
    /// Arm size value.
    /// </summary>
    public string ArmSize = string.Empty;

    /// <summary>
    /// Skin color value.
    /// </summary>
    public string SkinColor = string.Empty;

    /// <summary>
    /// Persona pieces.
    /// </summary>
    public List<PersonaPiece> PersonaPieces = [];

    /// <summary>
    /// Persona tint colors.
    /// </summary>
    public List<PersonaPieceTintColor> PieceTintColors = [];

    /// <summary>
    /// Whether this is a premium skin.
    /// </summary>
    public bool PremiumSkin;

    /// <summary>
    /// Whether this is a persona skin.
    /// </summary>
    public bool PersonaSkin;

    /// <summary>
    /// Whether persona cape is on classic skin.
    /// </summary>
    public bool PersonaCapeOnClassicSkin;

    /// <summary>
    /// Whether this is a primary user skin.
    /// </summary>
    public bool PrimaryUser;

    /// <summary>
    /// Whether this skin overrides appearance.
    /// </summary>
    public bool OverrideAppearance;

    /// <summary>
    /// Whether this skin is trusted.
    /// </summary>
    public bool Trusted;

    public void Read(BinaryReader reader)
    {
        SkinId = reader.ReadVarString();
        PlayFabId = reader.ReadVarString();
        SkinResourcePatch = SkinAnimation.ReadByteArray(reader);
        SkinImageWidth = reader.ReadUInt32(true);
        SkinImageHeight = reader.ReadUInt32(true);
        SkinData = SkinAnimation.ReadByteArray(reader);

        int animationCount = checked((int)reader.ReadUInt32(true));
        Animations = new List<SkinAnimation>(animationCount);
        for (int i = 0; i < animationCount; i++)
        {
            SkinAnimation animation = new();
            animation.Read(reader);
            Animations.Add(animation);
        }

        CapeImageWidth = reader.ReadUInt32(true);
        CapeImageHeight = reader.ReadUInt32(true);
        CapeData = SkinAnimation.ReadByteArray(reader);
        SkinGeometry = SkinAnimation.ReadByteArray(reader);
        GeometryDataEngineVersion = SkinAnimation.ReadByteArray(reader);
        AnimationData = SkinAnimation.ReadByteArray(reader);
        CapeId = reader.ReadVarString();
        FullId = reader.ReadVarString();
        ArmSize = reader.ReadVarString();
        SkinColor = reader.ReadVarString();

        int pieceCount = checked((int)reader.ReadUInt32(true));
        PersonaPieces = new List<PersonaPiece>(pieceCount);
        for (int i = 0; i < pieceCount; i++)
        {
            PersonaPiece piece = new();
            piece.Read(reader);
            PersonaPieces.Add(piece);
        }

        int tintCount = checked((int)reader.ReadUInt32(true));
        PieceTintColors = new List<PersonaPieceTintColor>(tintCount);
        for (int i = 0; i < tintCount; i++)
        {
            PersonaPieceTintColor tint = new();
            tint.Read(reader);
            PieceTintColors.Add(tint);
        }

        PremiumSkin = reader.ReadBool();
        PersonaSkin = reader.ReadBool();
        PersonaCapeOnClassicSkin = reader.ReadBool();
        PrimaryUser = reader.ReadBool();
        OverrideAppearance = reader.ReadBool();
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteVarString(SkinId);
        writer.WriteVarString(PlayFabId);
        SkinAnimation.WriteByteArray(writer, SkinResourcePatch);
        writer.WriteUInt32(SkinImageWidth, true);
        writer.WriteUInt32(SkinImageHeight, true);
        SkinAnimation.WriteByteArray(writer, SkinData);

        writer.WriteUInt32((uint)Animations.Count, true);
        for (int i = 0; i < Animations.Count; i++)
        {
            Animations[i].Write(writer);
        }

        writer.WriteUInt32(CapeImageWidth, true);
        writer.WriteUInt32(CapeImageHeight, true);
        SkinAnimation.WriteByteArray(writer, CapeData);
        SkinAnimation.WriteByteArray(writer, SkinGeometry);
        SkinAnimation.WriteByteArray(writer, GeometryDataEngineVersion);
        SkinAnimation.WriteByteArray(writer, AnimationData);
        writer.WriteVarString(CapeId);
        writer.WriteVarString(FullId);
        writer.WriteVarString(ArmSize);
        writer.WriteVarString(SkinColor);

        writer.WriteUInt32((uint)PersonaPieces.Count, true);
        for (int i = 0; i < PersonaPieces.Count; i++)
        {
            PersonaPieces[i].Write(writer);
        }

        writer.WriteUInt32((uint)PieceTintColors.Count, true);
        for (int i = 0; i < PieceTintColors.Count; i++)
        {
            PieceTintColors[i].Write(writer);
        }

        writer.WriteBool(PremiumSkin);
        writer.WriteBool(PersonaSkin);
        writer.WriteBool(PersonaCapeOnClassicSkin);
        writer.WriteBool(PrimaryUser);
        writer.WriteBool(OverrideAppearance);
    }
}
