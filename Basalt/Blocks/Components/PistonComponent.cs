using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class PistonComponent : BlockComponent {
    public static new string Identifier => "minecraft:piston";
    public override string ComponentIdentifier => "minecraft:piston";

    public bool IsMoving { get; }
    public string State { get; }

    public PistonComponent(bool isMoving = false, string state = "Retracted") {
        IsMoving = isMoving;
        State = state;
    }

    public static PistonComponent FromJson(JsonElement element) {
        bool isMoving = false;
        string state = "Retracted";

        if (element.TryGetProperty("isMoving", out JsonElement movingEl)) {
            isMoving = movingEl.ValueKind == JsonValueKind.True;
        }

        if (element.TryGetProperty("state", out JsonElement stateEl) && stateEl.ValueKind == JsonValueKind.String) {
            state = stateEl.GetString() ?? "Retracted";
        }

        return new PistonComponent(isMoving, state);
    }
}
