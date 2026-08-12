#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PhotoType {
    Portfolio = 0,
    PhotoItem = 1,
    Book = 2,
}

public static class PhotoTypeExtensions {
    public static string ToProtoString(this PhotoType value) => value.ToProtocolString();

    public static string ToProtocolString(this PhotoType value) {
        return value switch {
            PhotoType.Portfolio => "Portfolio",
            PhotoType.PhotoItem => "PhotoItem",
            PhotoType.Book => "Book",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PhotoType value.")
        };
    }

    public static PhotoType FromProtocolString(string value) {
        return value switch {
            "Portfolio" => PhotoType.Portfolio,
            "PhotoItem" => PhotoType.PhotoItem,
            "Book" => PhotoType.Book,
            _ => throw new ArgumentException($"Unknown PhotoType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PhotoType result) {
        switch (value) {
            case "Portfolio":
                result = PhotoType.Portfolio;
                return true;
            case "PhotoItem":
                result = PhotoType.PhotoItem;
                return true;
            case "Book":
                result = PhotoType.Book;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
