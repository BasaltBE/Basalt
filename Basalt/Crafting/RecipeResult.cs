namespace Basalt.Core.Crafting;

public sealed class RecipeResult
{
    public string Item { get; }
    public int Count { get; }
    public int Data { get; }

    public RecipeResult(string item, int count = 1, int data = 0)
    {
        Item = item;
        Count = count;
        Data = data;
    }
}
