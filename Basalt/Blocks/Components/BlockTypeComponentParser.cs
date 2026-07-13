namespace Basalt.Core.Blocks.Components;

using System.Text.Json;

public static class BlockComponentParser
{
    private static readonly HashSet<string> WarnedComponents = new(StringComparer.Ordinal);

    public static BlockComponent? Parse(string componentName, JsonElement element)
    {
        BlockComponent? result = componentName switch
        {
            "minecraft:movable" => MovableComponent.FromJson(element),
            "minecraft:precipitation_interactions" => new PrecipitationInteractionsComponent(),
            "minecraft:redstone_producer" => RedstoneProducerComponent.FromJson(element),
            "minecraft:sign" => SignComponent.FromJson(element),
            "minecraft:inventory" => InventoryComponent.FromJson(element),
            "minecraft:piston" => PistonComponent.FromJson(element),
            "minecraft:fluid_container" => FluidContainerComponent.FromJson(element),
            "minecraft:map_color" => MapColorComponent.FromJson(element),
            "minecraft:record_player" => new RecordPlayerComponent(),
            "minecraft:rotation" => new RotationComponent(),
            _ => null
        };

        if (result is null && WarnedComponents.Add(componentName))
        {
            Logger.Warn($"No data found for copmonent {componentName}");
        }

        return result;
    }
}
