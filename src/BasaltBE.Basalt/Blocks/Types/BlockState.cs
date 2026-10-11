using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.Blocks.Types;

[JsonConverter(typeof(BlockStateConverter))]
public sealed class BlockState {
    public Dictionary<string, object> Values = [];
}
