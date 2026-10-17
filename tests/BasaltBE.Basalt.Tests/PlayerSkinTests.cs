using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Network.Login;
using BasaltBE.Protocol.Enums;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class PlayerSkinTests
{
    [Fact]
    public void LoginBuildsFullSkinInsteadOfOnlyImageBytes()
    {
        ClientData data = new()
        {
            SkinId = "skin",
            SkinData = new byte[64 * 64 * 4],
            SkinImageWidth = 64,
            SkinImageHeight = 64,
            SkinResourcePatch = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"geometry\":{\"default\":\"geometry.humanoid.custom\"}}")),
            SkinGeometryData = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")),
            ArmSize = "slim",
            SkinColor = "#FF112233",
            PersonaPieces = [new ClientPersonaPiece { PieceId = "hair", PieceType = "persona_hair", IsDefault = true }],
            PieceTintColors = [new ClientPieceTint { PieceType = "persona_hand", Colors = ["#FF112233", "#FF000000", "#FFFFFFFF", "#FF123456"] }]
        };
        var skin = data.CreateSkin();
        Assert.Equal("skin", skin.Id);
        Assert.Equal(data.SkinId, skin.FullId);
        Assert.Contains("geometry.humanoid.custom", skin.ResourcePatch);
        Assert.Equal("{}", skin.GeometryData);
        Assert.Equal(PersonaArmSize.Slim, skin.ArmSize);
        Assert.Equal(unchecked((int)0xFF112233), skin.SkinColor);
        Assert.Equal(PersonaPieceType.Hair, skin.PersonaPieces[0].PieceType);
        Assert.True(skin.PieceTintColors.ContainsKey("hands"));
        Assert.Equal(TrustedSkinFlag.Unset, skin.TrustedSkinFlag);
    }

    [Fact]
    public void InvalidDimensionsAreRejected()
    {
        ClientData data = new() { SkinImageWidth = 64, SkinImageHeight = 64, SkinData = [0, 0, 0] };
        Assert.Throws<InvalidDataException>(() => data.CreateSkin());
    }

    [Fact]
    public void LoginReadsStandardAndUrlSafeSkinBase64IntoOwnedBytes()
    {
        byte[] image = [0xFB, 0xFF, 0xF0, 0x00];
        string standardBase64 = Convert.ToBase64String(image);
        string urlSafeBase64 = standardBase64.TrimEnd('=').Replace('+', '-').Replace('/', '_');

        foreach (string encodedImage in new[] { standardBase64, urlSafeBase64 })
        {
            byte[] json = Encoding.UTF8.GetBytes($"{{\"SkinData\":\"{encodedImage}\",\"SkinImageWidth\":1,\"SkinImageHeight\":1}}");
            ClientData data = JsonSerializer.Deserialize<ClientData>(json, LoginJson.Options)!;

            Assert.Equal(image, data.SkinData);
            Assert.Same(data.SkinData, data.CreateSkin().ImageData.ImageBytes);
        }
    }

    [Fact]
    public void OversizedImagesAreRejectedBeforeDecode()
    {
        ClientData data = new() { SkinImageWidth = uint.MaxValue, SkinImageHeight = uint.MaxValue };
        Assert.Throws<InvalidDataException>(() => data.CreateSkin());
    }
}
