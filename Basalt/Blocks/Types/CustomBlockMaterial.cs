namespace Basalt.Core.Blocks.Types;

public sealed class CustomBlockMaterial
{
    public required string Texture { get; init; }
    public string RenderMethod { get; init; } = "alpha_test";
    public bool FaceDimming { get; init; }
    public bool AmbientOcclusion { get; init; }
}
