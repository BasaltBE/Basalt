using System;

namespace BedrockProtocol.Enums;

public enum ShowStoreOfferRedirectType {
    MarketplaceOffer = 0,
    DressingRoomOffer = 1,
    ThirdPartyServerPage = 2,
}

public static class ShowStoreOfferRedirectTypeExtensions {
    public static string ToProtoString(this ShowStoreOfferRedirectType value) => value.ToProtocolString();

    public static string ToProtocolString(this ShowStoreOfferRedirectType value) {
        return value switch {
            ShowStoreOfferRedirectType.MarketplaceOffer => "MarketplaceOffer",
            ShowStoreOfferRedirectType.DressingRoomOffer => "DressingRoomOffer",
            ShowStoreOfferRedirectType.ThirdPartyServerPage => "ThirdPartyServerPage",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ShowStoreOfferRedirectType value.")
        };
    }

    public static ShowStoreOfferRedirectType FromProtocolString(string value) {
        return value switch {
            "MarketplaceOffer" => ShowStoreOfferRedirectType.MarketplaceOffer,
            "DressingRoomOffer" => ShowStoreOfferRedirectType.DressingRoomOffer,
            "ThirdPartyServerPage" => ShowStoreOfferRedirectType.ThirdPartyServerPage,
            _ => throw new ArgumentException($"Unknown ShowStoreOfferRedirectType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ShowStoreOfferRedirectType result) {
        switch (value) {
            case "MarketplaceOffer":
                result = ShowStoreOfferRedirectType.MarketplaceOffer;
                return true;
            case "DressingRoomOffer":
                result = ShowStoreOfferRedirectType.DressingRoomOffer;
                return true;
            case "ThirdPartyServerPage":
                result = ShowStoreOfferRedirectType.ThirdPartyServerPage;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
