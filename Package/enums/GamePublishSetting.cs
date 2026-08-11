using System;

namespace BedrockProtocol.Enums;

public enum GamePublishSetting {
    NoMultiPlay = 0,
    InviteOnly = 1,
    FriendsOnly = 2,
    FriendsOfFriends = 3,
    Public = 4,
}

public static class GamePublishSettingExtensions {
    public static string ToProtoString(this GamePublishSetting value) => value.ToProtocolString();

    public static string ToProtocolString(this GamePublishSetting value) {
        return value switch {
            GamePublishSetting.NoMultiPlay => "NoMultiPlay",
            GamePublishSetting.InviteOnly => "InviteOnly",
            GamePublishSetting.FriendsOnly => "FriendsOnly",
            GamePublishSetting.FriendsOfFriends => "FriendsOfFriends",
            GamePublishSetting.Public => "Public",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown GamePublishSetting value.")
        };
    }

    public static GamePublishSetting FromProtocolString(string value) {
        return value switch {
            "NoMultiPlay" => GamePublishSetting.NoMultiPlay,
            "InviteOnly" => GamePublishSetting.InviteOnly,
            "FriendsOnly" => GamePublishSetting.FriendsOnly,
            "FriendsOfFriends" => GamePublishSetting.FriendsOfFriends,
            "Public" => GamePublishSetting.Public,
            _ => throw new ArgumentException($"Unknown GamePublishSetting protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out GamePublishSetting result) {
        switch (value) {
            case "NoMultiPlay":
                result = GamePublishSetting.NoMultiPlay;
                return true;
            case "InviteOnly":
                result = GamePublishSetting.InviteOnly;
                return true;
            case "FriendsOnly":
                result = GamePublishSetting.FriendsOnly;
                return true;
            case "FriendsOfFriends":
                result = GamePublishSetting.FriendsOfFriends;
                return true;
            case "Public":
                result = GamePublishSetting.Public;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
