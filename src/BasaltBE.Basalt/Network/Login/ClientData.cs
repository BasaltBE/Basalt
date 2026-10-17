using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Login;

internal sealed class ClientData
{
    public string DeviceModel = string.Empty;
    public int DeviceOS;
    public string SelfSignedId = string.Empty;
    [JsonConverter(typeof(Base64UrlByteArrayConverter))]
    public byte[] SkinData = [];
    public string SkinId = string.Empty;
    public uint SkinImageWidth;
    public uint SkinImageHeight;
    public string SkinResourcePatch = string.Empty;
    public string SkinGeometryData = string.Empty;
    public string SkinGeometryDataEngineVersion = string.Empty;
    public string SkinAnimationData = string.Empty;
    public string CapeData = string.Empty;
    public uint CapeImageWidth;
    public uint CapeImageHeight;
    public string CapeId = string.Empty;
    public string ArmSize = string.Empty;
    public string SkinColor = string.Empty;
    public bool PremiumSkin;
    public bool PersonaSkin;
    public bool CapeOnClassicSkin;
    public bool OverrideSkin;
    public ClientSkinAnimation[] AnimatedImageData = [];
    public ClientPersonaPiece[] PersonaPieces = [];
    public ClientPieceTint[] PieceTintColors = [];

    internal SerializedSkin CreateSkin()
    {
        if (AnimatedImageData is null || PersonaPieces is null || PieceTintColors is null ||
            AnimatedImageData.Length > 128 || PersonaPieces.Length > 128 || PieceTintColors.Length > 128)
            throw new InvalidDataException("Skin collection exceeds the supported limit.");

        SerializedSkin skin = new()
        {
            Id = SkinId,
            FullId = SkinId,
            ResourcePatch = DecodeText(SkinResourcePatch),
            ImageData = DecodeSkinImage(SkinData, SkinImageWidth, SkinImageHeight),
            CapeImageData = DecodeImage(CapeData, CapeImageWidth, CapeImageHeight),
            GeometryData = DecodeText(SkinGeometryData),
            GeometryDataMinEngineVersion = DecodeText(SkinGeometryDataEngineVersion),
            AnimationData = DecodeText(SkinAnimationData),
            CapeId = CapeId,
            ArmSize = ArmSize == "slim" ? PersonaArmSize.Slim : PersonaArmSize.Wide,
            SkinColor = ParseColor(SkinColor),
            IsPremium = PremiumSkin,
            IsPersona = PersonaSkin,
            IsPersonaCapeOnClassicSkin = CapeOnClassicSkin,
            IsPrimaryUser = true,
            OverridesPlayerAppearance = OverrideSkin,
            TrustedSkinFlag = TrustedSkinFlag.Unset,
            AnimatedImageData = new AnimatedImageData[AnimatedImageData.Length],
            PersonaPieces = new SerializedPersonaPieceHandle[PersonaPieces.Length]
        };
        if (skin.ImageData.ImageBytes.Length == 0)
            throw new InvalidDataException("Player skin image is empty.");
        if (skin.ResourcePatch.Length != 0)
        {
            using JsonDocument patch = JsonDocument.Parse(skin.ResourcePatch);
        }
        if (skin.GeometryData.Length != 0)
        {
            using JsonDocument geometry = JsonDocument.Parse(skin.GeometryData);
        }
        for (int index = 0; index < AnimatedImageData.Length; index++)
        {
            ClientSkinAnimation animation = AnimatedImageData[index];
            if (!float.IsFinite(animation.Frames) || animation.Frames < 0 ||
                animation.Type > 3 || animation.AnimationExpression > 1)
                throw new InvalidDataException("Invalid skin animation.");
            skin.AnimatedImageData[index] = new AnimatedImageData
            {
                Image = DecodeImage(animation.Image, animation.ImageWidth, animation.ImageHeight),
                Frames = animation.Frames,
                TextureType = (PersonaAnimatedTextureType)animation.Type,
                Expression = (PersonaAnimationExpression)animation.AnimationExpression
            };
        }
        Span<byte> uuidBytes = stackalloc byte[16];
        for (int index = 0; index < PersonaPieces.Length; index++)
        {
            ClientPersonaPiece piece = PersonaPieces[index];
            ReadOnlySpan<char> name = piece.PieceType.AsSpan();
            if (name.StartsWith("persona_"))
                name = name[8..];
            Enum.TryParse(name, true, out PersonaPieceType pieceType);
            Uuid pack = new();
            if (Guid.TryParse(piece.PackId, out Guid packId))
            {
                packId.TryWriteBytes(uuidBytes, bigEndian: true, out _);
                pack.MostSignificantBits = BinaryPrimitives.ReadUInt64BigEndian(uuidBytes);
                pack.LeastSignificantBits = BinaryPrimitives.ReadUInt64BigEndian(uuidBytes[8..]);
            }
            skin.PersonaPieces[index] = new SerializedPersonaPieceHandle
            {
                PieceId = piece.PieceId,
                PieceType = pieceType,
                PackId = pack,
                IsDefaultPiece = piece.IsDefault,
                ProductId = piece.ProductId
            };
        }
        foreach (ClientPieceTint tint in PieceTintColors)
        {
            if (tint.Colors is null || tint.Colors.Length != 4)
                throw new InvalidDataException("Persona tint requires four colors.");
            string name = tint.PieceType.StartsWith("persona_") ? tint.PieceType[8..] : tint.PieceType;
            if (name == "hand")
                name = "hands";
            TintMapColor colors = new();
            for (int index = 0; index < 4; index++)
                colors.Colors[index] = ParseColor(tint.Colors[index]);
            skin.PieceTintColors.Add(name, colors);
        }
        return skin;
    }

    private static string DecodeText(string value) => string.IsNullOrEmpty(value)
        ? string.Empty
        : Encoding.UTF8.GetString(Base64Url.Decode(value.AsSpan()));

    private static SkinImage DecodeImage(string value, uint width, uint height)
    {
        if (width > 4096 || height > 4096 || (ulong)width * height * 4 > 2 * 1024 * 1024)
            throw new InvalidDataException("Skin image exceeds the size limit.");
        byte[] bytes = string.IsNullOrEmpty(value) ? [] : Base64Url.Decode(value.AsSpan());
        if ((ulong)bytes.Length != (ulong)width * height * 4)
            throw new InvalidDataException("Skin image dimensions do not match its bytes.");
        return new SkinImage { Width = width, Height = height, ImageBytes = bytes };
    }

    private static SkinImage DecodeSkinImage(byte[]? bytes, uint width, uint height)
    {
        if (width > 4096 || height > 4096 || (ulong)width * height * 4 > 2 * 1024 * 1024)
            throw new InvalidDataException("Skin image exceeds the size limit.");
        if (bytes is null || (ulong)bytes.Length != (ulong)width * height * 4)
            throw new InvalidDataException("Skin image dimensions do not match its bytes.");
        return new SkinImage { Width = width, Height = height, ImageBytes = bytes };
    }

    private static int ParseColor(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        ReadOnlySpan<char> hex = text.AsSpan();
        if (hex[0] == '#')
            hex = hex[1..];
        return unchecked((int)uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
