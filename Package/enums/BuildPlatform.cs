using System;

namespace BedrockProtocol.Enums;

public enum BuildPlatform {
    Google = 1,
    iOS = 2,
    OSX = 3,
    Amazon = 4,
    Win32 = 8,
    Dedicated = 9,
    Sony = 11,
    Nx = 12,
    Xbox = 13,
    Linux = 15,
    Unknown = -1,
}

public static class BuildPlatformExtensions {
    public static string ToProtoString(this BuildPlatform value) => value.ToProtocolString();

    public static string ToProtocolString(this BuildPlatform value) {
        return value switch {
            BuildPlatform.Google => "Google",
            BuildPlatform.iOS => "iOS",
            BuildPlatform.OSX => "OSX",
            BuildPlatform.Amazon => "Amazon",
            BuildPlatform.Win32 => "Win32",
            BuildPlatform.Dedicated => "Dedicated",
            BuildPlatform.Sony => "Sony",
            BuildPlatform.Nx => "Nx",
            BuildPlatform.Xbox => "Xbox",
            BuildPlatform.Linux => "Linux",
            BuildPlatform.Unknown => "Unknown",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BuildPlatform value.")
        };
    }

    public static BuildPlatform FromProtocolString(string value) {
        return value switch {
            "Google" => BuildPlatform.Google,
            "iOS" => BuildPlatform.iOS,
            "OSX" => BuildPlatform.OSX,
            "Amazon" => BuildPlatform.Amazon,
            "Win32" => BuildPlatform.Win32,
            "Dedicated" => BuildPlatform.Dedicated,
            "Sony" => BuildPlatform.Sony,
            "Nx" => BuildPlatform.Nx,
            "Xbox" => BuildPlatform.Xbox,
            "Linux" => BuildPlatform.Linux,
            "Unknown" => BuildPlatform.Unknown,
            _ => throw new ArgumentException($"Unknown BuildPlatform protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out BuildPlatform result) {
        switch (value) {
            case "Google":
                result = BuildPlatform.Google;
                return true;
            case "iOS":
                result = BuildPlatform.iOS;
                return true;
            case "OSX":
                result = BuildPlatform.OSX;
                return true;
            case "Amazon":
                result = BuildPlatform.Amazon;
                return true;
            case "Win32":
                result = BuildPlatform.Win32;
                return true;
            case "Dedicated":
                result = BuildPlatform.Dedicated;
                return true;
            case "Sony":
                result = BuildPlatform.Sony;
                return true;
            case "Nx":
                result = BuildPlatform.Nx;
                return true;
            case "Xbox":
                result = BuildPlatform.Xbox;
                return true;
            case "Linux":
                result = BuildPlatform.Linux;
                return true;
            case "Unknown":
                result = BuildPlatform.Unknown;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
