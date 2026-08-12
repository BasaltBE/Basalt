#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum HudElement {
    PaperDoll = 0,
    Armor = 1,
    ToolTips = 2,
    TouchControls = 3,
    Crosshair = 4,
    HotBar = 5,
    Health = 6,
    ProgressBar = 7,
    Hunger = 8,
    AirBubbles = 9,
    HorseHealth = 10,
    StatusEffects = 11,
    ItemText = 12,
}

public static class HudElementExtensions {
    public static string ToProtoString(this HudElement value) => value.ToProtocolString();

    public static string ToProtocolString(this HudElement value) {
        return value switch {
            HudElement.PaperDoll => "PaperDoll",
            HudElement.Armor => "Armor",
            HudElement.ToolTips => "ToolTips",
            HudElement.TouchControls => "TouchControls",
            HudElement.Crosshair => "Crosshair",
            HudElement.HotBar => "HotBar",
            HudElement.Health => "Health",
            HudElement.ProgressBar => "ProgressBar",
            HudElement.Hunger => "Hunger",
            HudElement.AirBubbles => "AirBubbles",
            HudElement.HorseHealth => "HorseHealth",
            HudElement.StatusEffects => "StatusEffects",
            HudElement.ItemText => "ItemText",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown HudElement value.")
        };
    }

    public static HudElement FromProtocolString(string value) {
        return value switch {
            "PaperDoll" => HudElement.PaperDoll,
            "Armor" => HudElement.Armor,
            "ToolTips" => HudElement.ToolTips,
            "TouchControls" => HudElement.TouchControls,
            "Crosshair" => HudElement.Crosshair,
            "HotBar" => HudElement.HotBar,
            "Health" => HudElement.Health,
            "ProgressBar" => HudElement.ProgressBar,
            "Hunger" => HudElement.Hunger,
            "AirBubbles" => HudElement.AirBubbles,
            "HorseHealth" => HudElement.HorseHealth,
            "StatusEffects" => HudElement.StatusEffects,
            "ItemText" => HudElement.ItemText,
            _ => throw new ArgumentException($"Unknown HudElement protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out HudElement result) {
        switch (value) {
            case "PaperDoll":
                result = HudElement.PaperDoll;
                return true;
            case "Armor":
                result = HudElement.Armor;
                return true;
            case "ToolTips":
                result = HudElement.ToolTips;
                return true;
            case "TouchControls":
                result = HudElement.TouchControls;
                return true;
            case "Crosshair":
                result = HudElement.Crosshair;
                return true;
            case "HotBar":
                result = HudElement.HotBar;
                return true;
            case "Health":
                result = HudElement.Health;
                return true;
            case "ProgressBar":
                result = HudElement.ProgressBar;
                return true;
            case "Hunger":
                result = HudElement.Hunger;
                return true;
            case "AirBubbles":
                result = HudElement.AirBubbles;
                return true;
            case "HorseHealth":
                result = HudElement.HorseHealth;
                return true;
            case "StatusEffects":
                result = HudElement.StatusEffects;
                return true;
            case "ItemText":
                result = HudElement.ItemText;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
