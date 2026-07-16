namespace Basalt.Core.Crafting;

public sealed class FurnaceRecipe
{
  public string Identifier { get; }
  public IReadOnlyList<string> Tags { get; }
  public string InputItem { get; }
  public string OutputItem { get; }

  public FurnaceRecipe(string identifier, IReadOnlyList<string> tags, string inputItem, string outputItem)
  {
    Identifier = identifier;
    Tags = tags;
    InputItem = inputItem;
    OutputItem = outputItem;
  }
}
