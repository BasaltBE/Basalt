namespace Basalt.Core.Crafting;

public sealed class CraftingRecipe
{
    public RecipeType Type { get; }
    public string Identifier { get; }
    public IReadOnlyList<string> Tags { get; }
    public int Priority { get; }

    // Shaped fields.
    public IReadOnlyList<string> Pattern { get; }
    public IReadOnlyDictionary<char, RecipeIngredient> Key { get; }

    // Shapeless fields.
    public IReadOnlyList<RecipeIngredient> Ingredients { get; }

    public RecipeResult Result { get; }

    public CraftingRecipe(
      RecipeType type,
      string identifier,
      IReadOnlyList<string> tags,
      int priority,
      IReadOnlyList<string> pattern,
      IReadOnlyDictionary<char, RecipeIngredient> key,
      IReadOnlyList<RecipeIngredient> ingredients,
      RecipeResult result)
    {
        Type = type;
        Identifier = identifier;
        Tags = tags;
        Priority = priority;
        Pattern = pattern;
        Key = key;
        Ingredients = ingredients;
        Result = result;
    }
}
