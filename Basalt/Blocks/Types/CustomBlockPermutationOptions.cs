namespace Basalt.Core.Blocks.Types;

public sealed class CustomBlockPermutationOptions {
    public required BlockState State { get; init; }
    public CustomBlockTransformation? Transformation { get; init; }
    public CustomBlockBox? SelectionBox { get; init; }
    public IReadOnlyList<CustomBlockBox>? CollisionBoxes { get; init; }
}
