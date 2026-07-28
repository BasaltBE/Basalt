namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Player;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class PlayerSkin {
    public static void Handle(Server server, NetworkConnection connection, PlayerSkinPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player? player)) {
            return;
        }

        if (packet.Skin.FullId == player.LastRequestedFullSkinId) {
            return;
        }

        player.LastRequestedFullSkinId = packet.Skin.FullId;
        Basalt.Protocol.Types.SerializedSkin skin = packet.Skin;
        Basalt.Protocol.Types.SerializedSkin updatedSkin = new() {
            Id = skin.Id,
            PlayFabId = skin.PlayFabId,
            ResourcePatch = skin.ResourcePatch,
            ImageData = new Basalt.Protocol.Types.SkinImage {
                Width = skin.ImageData.Width,
                Height = skin.ImageData.Height,
                Data = [.. skin.ImageData.Data]
            },
            AnimatedImageData = skin.AnimatedImageData.Select(animation => new Basalt.Protocol.Types.SkinAnimation {
                ImageWidth = animation.ImageWidth,
                ImageHeight = animation.ImageHeight,
                ImageData = [.. animation.ImageData],
                AnimationType = animation.AnimationType,
                FrameCount = animation.FrameCount,
                ExpressionType = animation.ExpressionType
            }).ToList(),
            CapeImageData = new Basalt.Protocol.Types.SkinImage {
                Width = skin.CapeImageData.Width,
                Height = skin.CapeImageData.Height,
                Data = [.. skin.CapeImageData.Data]
            },
            GeometryData = skin.GeometryData,
            GeometryDataMinEngineVersion = new Basalt.Protocol.Types.MinEngineVersion {
                Value = skin.GeometryDataMinEngineVersion.Value
            },
            AnimationData = skin.AnimationData,
            CapeId = skin.CapeId,
            FullId = Guid.NewGuid().ToString(),
            ArmSize = skin.ArmSize,
            SkinColor = skin.SkinColor,
            PersonaPieces = skin.PersonaPieces.Select(piece => new Basalt.Protocol.Types.SerializedPersonaPieceHandle {
                PieceId = piece.PieceId,
                PieceType = piece.PieceType,
                PackId = piece.PackId,
                Default = piece.Default,
                ProductId = piece.ProductId
            }).ToList(),
            PieceTintColors = skin.PieceTintColors.Select(tint => new Basalt.Protocol.Types.PersonaPieceTintColor {
                PieceType = tint.PieceType,
                Colors = [.. tint.Colors]
            }).ToList(),
            IsPremium = skin.IsPremium,
            IsPersona = skin.IsPersona,
            IsPersonaCapeOnClassicSkin = skin.IsPersonaCapeOnClassicSkin,
            IsPrimaryUser = skin.IsPrimaryUser,
            OverridesPlayerAppearance = skin.OverridesPlayerAppearance
        };

        player.LastRequestedFullSkinId = updatedSkin.FullId;
        player.SetSkin(Basalt.Protocol.Types.Skin.FromSerializedSkin(updatedSkin));
        PlayerSkinPacket skinPacket = new() {
            UUID = player.Uuid,
            Skin = updatedSkin,
            LocalizedNewSkinName = string.Empty,
            LocalizedOldSkinName = string.Empty,
            Verified = true
        };
        server.Broadcast(skinPacket);
    }
}
