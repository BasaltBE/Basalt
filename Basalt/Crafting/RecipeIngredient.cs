namespace Basalt.Core.Crafting;

public sealed class RecipeIngredient
{
    public string? Item { get; }
    public string? Tag { get; }
    public int Data { get; }
    public int Count { get; }

    public RecipeIngredient(string? item = null, string? tag = null, int data = 0, int count = 1)
    {
        Item = item;
        Tag = tag;
        Data = data;
        Count = count;
    }
}
