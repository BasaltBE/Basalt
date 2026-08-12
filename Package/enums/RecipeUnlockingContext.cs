#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum RecipeUnlockingContext {
    None = 0,
    AlwaysUnlocked = 1,
    PlayerInWater = 2,
    PlayerHasManyItems = 3,
}

public static class RecipeUnlockingContextExtensions {
    public static string ToProtoString(this RecipeUnlockingContext value) => value.ToProtocolString();

    public static string ToProtocolString(this RecipeUnlockingContext value) {
        return value switch {
            RecipeUnlockingContext.None => "None",
            RecipeUnlockingContext.AlwaysUnlocked => "AlwaysUnlocked",
            RecipeUnlockingContext.PlayerInWater => "PlayerInWater",
            RecipeUnlockingContext.PlayerHasManyItems => "PlayerHasManyItems",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown RecipeUnlockingContext value.")
        };
    }

    public static RecipeUnlockingContext FromProtocolString(string value) {
        return value switch {
            "None" => RecipeUnlockingContext.None,
            "AlwaysUnlocked" => RecipeUnlockingContext.AlwaysUnlocked,
            "PlayerInWater" => RecipeUnlockingContext.PlayerInWater,
            "PlayerHasManyItems" => RecipeUnlockingContext.PlayerHasManyItems,
            _ => throw new ArgumentException($"Unknown RecipeUnlockingContext protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out RecipeUnlockingContext result) {
        switch (value) {
            case "None":
                result = RecipeUnlockingContext.None;
                return true;
            case "AlwaysUnlocked":
                result = RecipeUnlockingContext.AlwaysUnlocked;
                return true;
            case "PlayerInWater":
                result = RecipeUnlockingContext.PlayerInWater;
                return true;
            case "PlayerHasManyItems":
                result = RecipeUnlockingContext.PlayerHasManyItems;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
