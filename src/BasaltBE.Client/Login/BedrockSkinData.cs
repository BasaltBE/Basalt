namespace BasaltBE.Client.Login;

internal class BedrockSkinData {
    public AnimatedImage[] AnimatedImageData = [];
    public string ArmSize = string.Empty;
    public string CapeData = string.Empty;
    public string CapeId = string.Empty;
    public int CapeImageHeight;
    public int CapeImageWidth;
    public bool CapeOnClassicSkin;
    public bool OverrideSkin;
    public PersonaPiece[] PersonaPieces = [];
    public bool PersonaSkin;
    public PieceTint[] PieceTintColors = [];
    public bool PremiumSkin;
    public string SkinAnimationData = string.Empty;
    public string SkinColor = string.Empty;
    public string SkinData = string.Empty;
    public string SkinGeometryData = string.Empty;
    public string SkinGeometryDataEngineVersion = string.Empty;
    public string SkinId = string.Empty;
    public int SkinImageHeight;
    public int SkinImageWidth;
    public string SkinResourcePatch = string.Empty;
    public bool TrustedSkin;

    public sealed class AnimatedImage {
        public int AnimationExpression = 0;
        public float Frames = 0;
        public string Image = string.Empty;
        public int ImageHeight = 0;
        public int ImageWidth = 0;
        public int Type = 0;
    }

    public sealed class PersonaPiece {
        public bool IsDefault = false;
        public string PackId = string.Empty;
        public string PieceId = string.Empty;
        public string PieceType = string.Empty;
        public string ProductId = string.Empty;
    }

    public sealed class PieceTint {
        public string[] Colors = [];
        public string PieceType = string.Empty;
    }
}

internal sealed class BedrockClientData : BedrockSkinData {
    public int ClientEditorConnectionIntent;
    public bool ClientIsEditorCapable;
    public long ClientRandomId;
    public bool CompatibleWithClientSideChunkGen;
    public int CurrentInputMode;
    public int DefaultInputMode;
    public string DeviceId = string.Empty;
    public string DeviceModel = string.Empty;
    public int DeviceOS;
    public bool FilterProfanity;
    public string GameVersion = string.Empty;
    public int GraphicsMode;
    public int GuiScale;
    public string LanguageCode = string.Empty;
    public int MaxViewDistance;
    public int MemoryTier;
    public string PlatformOfflineId = string.Empty;
    public string PlatformOnlineId = string.Empty;
    public int PlatformType;
    public string ProfileHash = string.Empty;
    public string SelfSignedId = string.Empty;
    public string ServerAddress = string.Empty;
    public string ThirdPartyName = string.Empty;
    public int UIProfile;
}
