using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class SerializedSkin : DataType {
    /// <summary>The player's skin identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The player's PlayFab identifier.</summary>
    public string PlayFabId = string.Empty;
    /// <summary>The player's resource patch.</summary>
    public string ResourcePatch = string.Empty;
    /// <summary>The player's skin image.</summary>
    public SkinImage ImageData = new();
    /// <summary>The player's animated skin images.</summary>
    public List<SkinAnimation> AnimatedImageData = [];
    /// <summary>The player's cape image.</summary>
    public SkinImage CapeImageData = new();
    /// <summary>The player's geometry data.</summary>
    public string GeometryData = string.Empty;
    /// <summary>The minimum engine version for the geometry.</summary>
    public MinEngineVersion GeometryDataMinEngineVersion = new();
    /// <summary>The player's animation data.</summary>
    public string AnimationData = string.Empty;
    /// <summary>The player's cape identifier.</summary>
    public string CapeId = string.Empty;
    /// <summary>The player's full skin identifier.</summary>
    public string FullId = string.Empty;
    /// <summary>The player's arm size.</summary>
    public string ArmSize = string.Empty;
    /// <summary>The player's skin color.</summary>
    public string SkinColor = string.Empty;
    /// <summary>The persona pieces used by the skin.</summary>
    public List<SerializedPersonaPieceHandle> PersonaPieces = [];
    /// <summary>The tint colors used by persona pieces.</summary>
    public List<PersonaPieceTintColor> PieceTintColors = [];
    /// <summary>Whether the skin is premium.</summary>
    public bool IsPremium;
    /// <summary>Whether the skin is a persona skin.</summary>
    public bool IsPersona;
    /// <summary>Whether the persona cape appears on a classic skin.</summary>
    public bool IsPersonaCapeOnClassicSkin;
    /// <summary>Whether the skin belongs to the primary user.</summary>
    public bool IsPrimaryUser;
    /// <summary>Whether the skin overrides the player's appearance.</summary>
    public bool OverridesPlayerAppearance;
    public void Read(BinaryReader reader) {
        Id = reader.ReadVarString();
        PlayFabId = reader.ReadVarString();
        ResourcePatch = reader.ReadVarString();
        ImageData.Read(reader);
        int animationCount = checked((int)reader.ReadUInt32(true));
        AnimatedImageData = new List<SkinAnimation>(animationCount);
        for (int i = 0; i < animationCount; i++) {
            SkinAnimation animation = new();
            animation.Read(reader);
            AnimatedImageData.Add(animation);
        }

        CapeImageData.Read(reader);
        GeometryData = reader.ReadVarString();
        GeometryDataMinEngineVersion.Read(reader);
        AnimationData = reader.ReadVarString();
        CapeId = reader.ReadVarString();
        FullId = reader.ReadVarString();
        ArmSize = reader.ReadVarString();
        SkinColor = reader.ReadVarString();

        int personaCount = checked((int)reader.ReadUInt32(true));
        PersonaPieces = new List<SerializedPersonaPieceHandle>(personaCount);
        for (int i = 0; i < personaCount; i++) {
            SerializedPersonaPieceHandle piece = new();
            piece.Read(reader);
            PersonaPieces.Add(piece);
        }

        int tintCount = checked((int)reader.ReadUInt32(true));
        PieceTintColors = new List<PersonaPieceTintColor>(tintCount);
        for (int i = 0; i < tintCount; i++) {
            PersonaPieceTintColor tint = new();
            tint.Read(reader);
            PieceTintColors.Add(tint);
        }

        IsPremium = reader.ReadBool();
        IsPersona = reader.ReadBool();
        IsPersonaCapeOnClassicSkin = reader.ReadBool();
        IsPrimaryUser = reader.ReadBool();
        OverridesPlayerAppearance = reader.ReadBool();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(Id);
        writer.WriteVarString(PlayFabId);
        writer.WriteVarString(ResourcePatch);
        ImageData.Write(writer);
        writer.WriteUInt32((uint)AnimatedImageData.Count, true);
        for (int i = 0; i < AnimatedImageData.Count; i++) {
            AnimatedImageData[i].Write(writer);
        }

        CapeImageData.Write(writer);
        writer.WriteVarString(GeometryData);
        GeometryDataMinEngineVersion.Write(writer);
        writer.WriteVarString(AnimationData);
        writer.WriteVarString(CapeId);
        writer.WriteVarString(FullId);
        writer.WriteVarString(ArmSize);
        writer.WriteVarString(SkinColor);
        writer.WriteUInt32((uint)PersonaPieces.Count, true);
        for (int i = 0; i < PersonaPieces.Count; i++) {
            PersonaPieces[i].Write(writer);
        }

        writer.WriteUInt32((uint)PieceTintColors.Count, true);
        for (int i = 0; i < PieceTintColors.Count; i++) {
            PieceTintColors[i].Write(writer);
        }

        writer.WriteBool(IsPremium);
        writer.WriteBool(IsPersona);
        writer.WriteBool(IsPersonaCapeOnClassicSkin);
        writer.WriteBool(IsPrimaryUser);
        writer.WriteBool(OverridesPlayerAppearance);
    }
}
