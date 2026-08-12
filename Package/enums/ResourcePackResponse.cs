#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ResourcePackResponse {
    Cancel = 0,
    Downloading = 1,
    DownloadingFinished = 2,
    ResourcePackStackFinished = 3,
}

public static class ResourcePackResponseExtensions {
    public static string ToProtoString(this ResourcePackResponse value) => value.ToProtocolString();

    public static string ToProtocolString(this ResourcePackResponse value) {
        return value switch {
            ResourcePackResponse.Cancel => "Cancel",
            ResourcePackResponse.Downloading => "Downloading",
            ResourcePackResponse.DownloadingFinished => "DownloadingFinished",
            ResourcePackResponse.ResourcePackStackFinished => "ResourcePackStackFinished",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ResourcePackResponse value.")
        };
    }

    public static ResourcePackResponse FromProtocolString(string value) {
        return value switch {
            "Cancel" => ResourcePackResponse.Cancel,
            "Downloading" => ResourcePackResponse.Downloading,
            "DownloadingFinished" => ResourcePackResponse.DownloadingFinished,
            "ResourcePackStackFinished" => ResourcePackResponse.ResourcePackStackFinished,
            _ => throw new ArgumentException($"Unknown ResourcePackResponse protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ResourcePackResponse result) {
        switch (value) {
            case "Cancel":
                result = ResourcePackResponse.Cancel;
                return true;
            case "Downloading":
                result = ResourcePackResponse.Downloading;
                return true;
            case "DownloadingFinished":
                result = ResourcePackResponse.DownloadingFinished;
                return true;
            case "ResourcePackStackFinished":
                result = ResourcePackResponse.ResourcePackStackFinished;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
