using System.Text.Json;

namespace Basalt.Core.Blocks.Components;

public sealed class RedstoneProducerComponent : BlockComponent {
    public static new string Identifier => "minecraft:redstone_producer";
    public override string ComponentIdentifier => "minecraft:redstone_producer";

    public int Power { get; }

    public RedstoneProducerComponent(int power = 0) {
        Power = power;
    }

    public static RedstoneProducerComponent FromJson(JsonElement element) {
        int power = 0;
        if (element.TryGetProperty("power", out JsonElement powerEl) && powerEl.ValueKind == JsonValueKind.Number) {
            power = powerEl.GetInt32();
        }

        return new RedstoneProducerComponent(power);
    }
}
