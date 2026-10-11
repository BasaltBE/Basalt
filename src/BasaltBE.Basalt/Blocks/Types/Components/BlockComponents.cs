using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types.Components;

public sealed class BlockComponents {
    [JsonInclude, JsonPropertyName("minecraft:fluid_container")]
    public FluidContainerComponent? FluidContainer;

    [JsonInclude, JsonPropertyName("minecraft:instrument_sound")]
    public InstrumentSoundComponent? InstrumentSound;

    [JsonInclude, JsonPropertyName("minecraft:inventory")]
    public InventoryComponent? Inventory;

    [JsonInclude, JsonPropertyName("minecraft:map_color")]
    public MapColorComponent? MapColor;

    [JsonInclude, JsonPropertyName("minecraft:movable")]
    public MovableComponent? Movable;

    [JsonInclude, JsonPropertyName("minecraft:piston")]
    public PistonComponent? Piston;

    [JsonInclude, JsonPropertyName("minecraft:precipitation_interactions")]
    public PrecipitationInteractionsComponent? PrecipitationInteractions;

    [JsonInclude, JsonPropertyName("minecraft:record_player")]
    public RecordPlayerComponent? RecordPlayer;

    [JsonInclude, JsonPropertyName("minecraft:redstone_producer")]
    public RedstoneProducerComponent? RedstoneProducer;

    [JsonInclude, JsonPropertyName("minecraft:sign")]
    public SignComponent? Sign;
}
