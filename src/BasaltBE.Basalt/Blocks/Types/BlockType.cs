using System.Text.Json.Serialization;
using BasaltBE.Basalt.Blocks.Types.Components;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Blocks.Types;

public sealed class BlockType {
    [JsonIgnore]
    internal BlockCollisionBox[]? CollisionBoxes;

    [JsonIgnore]
    internal ToolType ToolTypes;

    [JsonInclude, JsonPropertyName("identifier")]
    public string Identifier = string.Empty;

    [JsonInclude, JsonPropertyName("states")]
    public string[] States = [];

    [JsonInclude, JsonPropertyName("components")]
    public BlockComponents Components = new();

    [JsonInclude, JsonPropertyName("tags")]
    public string[] Tags = [];

    [JsonInclude, JsonPropertyName("air")]
    public bool Air;

    [JsonInclude, JsonPropertyName("liquid")]
    public bool Liquid;

    [JsonInclude, JsonPropertyName("solid")]
    public bool Solid;

    [JsonInclude, JsonPropertyName("blastResistance")]
    public float BlastResistance;

    [JsonInclude, JsonPropertyName("brightness")]
    public int Brightness;

    [JsonInclude, JsonPropertyName("flameEncouragement")]
    public int FlameEncouragement;

    [JsonInclude, JsonPropertyName("flammability")]
    public int Flammability;

    [JsonInclude, JsonPropertyName("friction")]
    public float Friction;

    [JsonInclude, JsonPropertyName("hardness")]
    public float Hardness;

    [JsonInclude, JsonPropertyName("opacity")]
    public float Opacity;
}
