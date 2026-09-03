using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class SignComponent : BlockComponent {
    public static new string Identifier => "minecraft:sign";
    public override string ComponentIdentifier => "minecraft:sign";

    public bool IsWaxed { get; }

    public SignComponent(bool isWaxed = false) {
        IsWaxed = isWaxed;
    }

    public static SignComponent FromJson(JsonElement element) {
        bool isWaxed = false;
        if (element.TryGetProperty("isWaxed", out JsonElement waxedEl)) {
            isWaxed = waxedEl.ValueKind == JsonValueKind.True;
        }

        return new SignComponent(isWaxed);
    }
}
