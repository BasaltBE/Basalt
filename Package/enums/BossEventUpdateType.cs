using System;

namespace BedrockProtocol.Enums;

public enum BossEventUpdateType {
    Add = 0,
    PlayerAdded = 1,
    Remove = 2,
    PlayerRemoved = 3,
    Update_Percent = 4,
    Update_Name = 5,
    Update_Properties = 6,
    Update_Style = 7,
    Query = 8,
}

public static class BossEventUpdateTypeExtensions {
    public static string ToProtoString(this BossEventUpdateType value) => value.ToProtocolString();

    public static string ToProtocolString(this BossEventUpdateType value) {
        return value switch {
            BossEventUpdateType.Add => "Add",
            BossEventUpdateType.PlayerAdded => "PlayerAdded",
            BossEventUpdateType.Remove => "Remove",
            BossEventUpdateType.PlayerRemoved => "PlayerRemoved",
            BossEventUpdateType.Update_Percent => "Update_Percent",
            BossEventUpdateType.Update_Name => "Update_Name",
            BossEventUpdateType.Update_Properties => "Update_Properties",
            BossEventUpdateType.Update_Style => "Update_Style",
            BossEventUpdateType.Query => "Query",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown BossEventUpdateType value.")
        };
    }

    public static BossEventUpdateType FromProtocolString(string value) {
        return value switch {
            "Add" => BossEventUpdateType.Add,
            "PlayerAdded" => BossEventUpdateType.PlayerAdded,
            "Remove" => BossEventUpdateType.Remove,
            "PlayerRemoved" => BossEventUpdateType.PlayerRemoved,
            "Update_Percent" => BossEventUpdateType.Update_Percent,
            "Update_Name" => BossEventUpdateType.Update_Name,
            "Update_Properties" => BossEventUpdateType.Update_Properties,
            "Update_Style" => BossEventUpdateType.Update_Style,
            "Query" => BossEventUpdateType.Query,
            _ => throw new ArgumentException($"Unknown BossEventUpdateType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out BossEventUpdateType result) {
        switch (value) {
            case "Add":
                result = BossEventUpdateType.Add;
                return true;
            case "PlayerAdded":
                result = BossEventUpdateType.PlayerAdded;
                return true;
            case "Remove":
                result = BossEventUpdateType.Remove;
                return true;
            case "PlayerRemoved":
                result = BossEventUpdateType.PlayerRemoved;
                return true;
            case "Update_Percent":
                result = BossEventUpdateType.Update_Percent;
                return true;
            case "Update_Name":
                result = BossEventUpdateType.Update_Name;
                return true;
            case "Update_Properties":
                result = BossEventUpdateType.Update_Properties;
                return true;
            case "Update_Style":
                result = BossEventUpdateType.Update_Style;
                return true;
            case "Query":
                result = BossEventUpdateType.Query;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
