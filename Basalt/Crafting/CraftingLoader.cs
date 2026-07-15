namespace Basalt.Core.Crafting;

using System.Text.Json;

public static class CraftingLoader
{
  public static void Load(string? dataDirectory = null)
  {
    CraftingRegistry.Initialize();
    string root = ResolveDataRoot(dataDirectory);
    string recipesPath = Path.Combine(root, "crafting_recipes.json");

    if (!File.Exists(recipesPath))
    {
      Logger.Warn($"Crafting: recipe file not found at '{recipesPath}'.");
      return;
    }

    using FileStream stream = File.OpenRead(recipesPath);
    using JsonDocument document = JsonDocument.Parse(stream);

    if (document.RootElement.ValueKind != JsonValueKind.Array)
    {
      Logger.Warn("Crafting: recipe file root is not an array.");
      return;
    }

    int loaded = 0;
    int skipped = 0;

    foreach (JsonElement element in document.RootElement.EnumerateArray())
    {
      CraftingRecipe? recipe = ParseRecipe(element);
      if (recipe is null)
      {
        skipped++;
        continue;
      }

      CraftingRegistry.Instance.AddRecipe(recipe);
      loaded++;
    }

    Logger.Info($"Crafting: parsed {loaded} recipes ({skipped} skipped).");
  }

  private static CraftingRecipe? ParseRecipe(JsonElement element)
  {
    string typeStr = ReadString(element, "type");
    RecipeType type = typeStr switch
    {
      "shaped" => RecipeType.Shaped,
      "shapeless" => RecipeType.Shapeless,
      _ => (RecipeType)(-1)
    };

    if ((int)type == -1) return null;

    string identifier = ReadString(element, "identifier");
    if (string.IsNullOrEmpty(identifier)) return null;

    List<string> tags = ParseStringArray(element, "tags");
    int priority = ReadInt(element, "priority", 0);

    List<string> pattern = [];
    Dictionary<char, RecipeIngredient> key = [];
    List<RecipeIngredient> ingredients = [];

    switch (type)
    {
      case RecipeType.Shaped:
        pattern = ParseStringArray(element, "pattern");
        key = ParseKey(element);
        break;
      case RecipeType.Shapeless:
        ingredients = ParseIngredients(element);
        break;
    }

    RecipeResult? result = ParseResult(element);
    if (result is null) return null;

    return new CraftingRecipe(type, identifier, tags, priority, pattern, key, ingredients, result);
  }

  private static Dictionary<char, RecipeIngredient> ParseKey(JsonElement element)
  {
    Dictionary<char, RecipeIngredient> key = [];
    if (!element.TryGetProperty("key", out JsonElement keyElement) || keyElement.ValueKind != JsonValueKind.Object)
    {
      return key;
    }

    foreach (JsonProperty property in keyElement.EnumerateObject())
    {
      if (property.Name.Length != 1) continue;
      char symbol = property.Name[0];
      RecipeIngredient? ingredient = ParseIngredient(property.Value);
      if (ingredient is not null)
      {
        key[symbol] = ingredient;
      }
    }

    return key;
  }

  private static List<RecipeIngredient> ParseIngredients(JsonElement element)
  {
    List<RecipeIngredient> ingredients = [];
    if (!element.TryGetProperty("ingredients", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
    {
      return ingredients;
    }

    foreach (JsonElement item in array.EnumerateArray())
    {
      RecipeIngredient? ingredient = ParseIngredient(item);
      if (ingredient is not null)
      {
        ingredients.Add(ingredient);
      }
    }

    return ingredients;
  }

  private static RecipeIngredient? ParseIngredient(JsonElement element)
  {
    if (element.ValueKind != JsonValueKind.Object) return null;

    string? item = element.TryGetProperty("item", out JsonElement itemEl) && itemEl.ValueKind == JsonValueKind.String
      ? itemEl.GetString()
      : null;

    string? tag = element.TryGetProperty("tag", out JsonElement tagEl) && tagEl.ValueKind == JsonValueKind.String
      ? tagEl.GetString()
      : null;

    int data = ReadInt(element, "data", 0);
    int count = ReadInt(element, "count", 1);

    if (item is null && tag is null) return null;

    return new RecipeIngredient(item, tag, data, count);
  }

  private static RecipeResult? ParseResult(JsonElement element)
  {
    if (!element.TryGetProperty("result", out JsonElement resultEl)) return null;

    JsonElement target = resultEl;
    if (resultEl.ValueKind == JsonValueKind.Array)
    {
      if (resultEl.GetArrayLength() == 0) return null;
      target = resultEl[0];
    }

    if (target.ValueKind != JsonValueKind.Object) return null;

    string item = ReadString(target, "item");
    if (string.IsNullOrEmpty(item)) return null;

    int count = ReadInt(target, "count", 1);
    int data = ReadInt(target, "data", 0);

    return new RecipeResult(item, count, data);
  }

  private static List<string> ParseStringArray(JsonElement element, string property)
  {
    List<string> result = [];
    if (!element.TryGetProperty(property, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
    {
      return result;
    }

    foreach (JsonElement item in array.EnumerateArray())
    {
      if (item.ValueKind == JsonValueKind.String)
      {
        result.Add(item.GetString() ?? string.Empty);
      }
    }

    return result;
  }

  private static string ReadString(JsonElement element, string property)
  {
    return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
      ? value.GetString() ?? string.Empty
      : string.Empty;
  }

  private static int ReadInt(JsonElement element, string property, int fallback)
  {
    return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result)
      ? result
      : fallback;
  }

  private static string ResolveDataRoot(string? dataDirectory)
  {
    if (!string.IsNullOrWhiteSpace(dataDirectory)) return dataDirectory;

    string? current = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(current))
    {
      string candidate = Path.Combine(current, "Protocol", "Data");
      if (Directory.Exists(candidate)) return candidate;

      DirectoryInfo? parent = Directory.GetParent(current);
      if (parent is null) break;
      current = parent.FullName;
    }

    throw new DirectoryNotFoundException("Could not locate Protocol/Data directory.");
  }
}
