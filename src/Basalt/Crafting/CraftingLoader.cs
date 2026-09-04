namespace Basalt.Core.Crafting;

using System.Text.Json;
using Basalt.Core.Profiling;

public static class CraftingLoader {
    private static readonly object SyncRoot = new();

    public static void Load(string? dataDirectory = null) {
        lock (SyncRoot) {
            using var _ = Profiler.Enabled ? Profiler.BeginZone("Crafting.Load") : default;
            CraftingRegistry.Initialize();
            FurnaceRegistry.Initialize();

        Stream? stream;
        if (!string.IsNullOrWhiteSpace(dataDirectory)) {
            string recipesPath = Path.Combine(dataDirectory, "recipes.json");
            if (!File.Exists(recipesPath)) {
                recipesPath = Path.Combine(dataDirectory, "crafting_recipes.json");
            }
            if (!File.Exists(recipesPath)) {
                Logger.Warn($"Crafting: recipe file not found at '{recipesPath}'.");
                return;
            }

            stream = File.OpenRead(recipesPath);
        }
        else {
            stream = ProtocolData.Open("recipes.json") ?? ProtocolData.Open("crafting_recipes.json");
            if (stream is null) {
                Logger.Warn("Crafting: embedded recipe resource not found.");
                return;
            }
        }

        using (stream) {
            using JsonDocument document = JsonDocument.Parse(stream);

            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("recipes", out JsonElement recipes) &&
                recipes.ValueKind == JsonValueKind.Array) {
                LoadCurrent(recipes);
                return;
            }

            if (document.RootElement.ValueKind != JsonValueKind.Array) {
                Logger.Warn("Crafting: recipe file does not contain a recipes array.");
                return;
            }

            LoadLegacy(document.RootElement);
        }
    }
    }

    private static void LoadCurrent(JsonElement recipes) {
        foreach (JsonElement element in recipes.EnumerateArray()) {
            int type = ReadInt(element, "type", -1);
            if (type is 1) {
                CraftingRecipe? recipe = ParseCurrentShaped(element);
                if (recipe is not null) {
                    CraftingRegistry.Instance.AddRecipe(recipe);
                }

                continue;
            }

            if (type == 0 && IsFurnaceRecipe(element)) {
                FurnaceRecipe? furnace = ParseCurrentFurnace(element);
                if (furnace is not null) {
                    FurnaceRegistry.Instance.Register(furnace);
                }

                continue;
            }

            if (type is 0 or 5) {
                CraftingRecipe? recipe = ParseCurrentShapeless(element);
                if (recipe is not null) {
                    CraftingRegistry.Instance.AddRecipe(recipe);
                }
            }
        }
    }

    private static bool IsFurnaceRecipe(JsonElement element) {
        string block = ReadString(element, "block");
        if (block is "furnace" or "blast_furnace" or "smoker" or "campfire" or "soul_campfire") {
            return true;
        }

        return block == "deprecated" && ReadString(element, "id").StartsWith("minecraft:furnace_", StringComparison.Ordinal);
    }

    private static FurnaceRecipe? ParseCurrentFurnace(JsonElement element) {
        string identifier = ReadString(element, "id");
        if (string.IsNullOrEmpty(identifier) ||
            !element.TryGetProperty("input", out JsonElement input) ||
            input.ValueKind != JsonValueKind.Array || input.GetArrayLength() == 0 ||
            !element.TryGetProperty("output", out JsonElement output) ||
            output.ValueKind != JsonValueKind.Array || output.GetArrayLength() == 0) {
            return null;
        }

        RecipeIngredient? inputIngredient = ParseCurrentIngredient(input[0]);
        string outputItem = ReadString(output[0], "id");
        if (inputIngredient?.Item is null || string.IsNullOrEmpty(outputItem)) {
            return null;
        }

        string block = ReadString(element, "block");
        string tag = block == "deprecated" ? "furnace" : block;
        return new FurnaceRecipe(identifier, [tag], inputIngredient.Item, outputItem);
    }

    private static void LoadLegacy(JsonElement recipes) {
        foreach (JsonElement element in recipes.EnumerateArray()) {
                string type = ReadString(element, "type");

                if (type == "furnace") {
                    FurnaceRecipe? furnace = ParseFurnaceRecipe(element);
                    if (furnace is null) {
                        continue;
                    }

                    FurnaceRegistry.Instance.Register(furnace);
                    continue;
                }

                CraftingRecipe? recipe = ParseRecipe(element);
                if (recipe is null) {
                    continue;
                }

                CraftingRegistry.Instance.AddRecipe(recipe);
            }
    }

    private static CraftingRecipe? ParseCurrentShaped(JsonElement element) {
        string identifier = ReadString(element, "id");
        List<string> pattern = ParseStringArray(element, "shape");
        if (string.IsNullOrEmpty(identifier) || pattern.Count == 0 ||
            !element.TryGetProperty("input", out JsonElement input) ||
            input.ValueKind != JsonValueKind.Object) {
            return null;
        }

        Dictionary<char, RecipeIngredient> key = [];
        foreach (JsonProperty property in input.EnumerateObject()) {
            if (property.Name.Length != 1) {
                continue;
            }

            RecipeIngredient? ingredient = ParseCurrentIngredient(property.Value);
            if (ingredient is not null) {
                key[property.Name[0]] = ingredient;
            }
        }

        RecipeResult? result = ParseCurrentResult(element);
        return result is null
            ? null
            : new CraftingRecipe(
                RecipeType.Shaped,
                identifier,
                ResolveTags(element),
                ReadInt(element, "priority", 0),
                pattern,
                key,
                [],
                result);
    }

    private static CraftingRecipe? ParseCurrentShapeless(JsonElement element) {
        string identifier = ReadString(element, "id");
        if (string.IsNullOrEmpty(identifier) ||
            !element.TryGetProperty("input", out JsonElement input) ||
            input.ValueKind != JsonValueKind.Array) {
            return null;
        }

        List<RecipeIngredient> ingredients = [];
        foreach (JsonElement value in input.EnumerateArray()) {
            RecipeIngredient? ingredient = ParseCurrentIngredient(value);
            if (ingredient is not null) {
                ingredients.Add(ingredient);
            }
        }

        RecipeResult? result = ParseCurrentResult(element);
        return result is null
            ? null
            : new CraftingRecipe(
                RecipeType.Shapeless,
                identifier,
                ResolveTags(element),
                ReadInt(element, "priority", 0),
                [],
                new Dictionary<char, RecipeIngredient>(),
                ingredients,
                result);
    }

    private static RecipeIngredient? ParseCurrentIngredient(JsonElement element) {
        if (element.ValueKind != JsonValueKind.Object) {
            return null;
        }

        string item = ReadString(element, "id");
        string tag = ReadString(element, "itemTag");
        int count = ReadInt(element, "count", 1);
        int data = ReadInt(element, "auxValue", 0);
        if (data == 0x7FFF) {
            data = 0;
        }

        return string.IsNullOrEmpty(item) && string.IsNullOrEmpty(tag)
            ? null
            : new RecipeIngredient(
                string.IsNullOrEmpty(item) ? null : item,
                string.IsNullOrEmpty(tag) ? null : tag,
                data,
                count);
    }

    private static RecipeResult? ParseCurrentResult(JsonElement element) {
        if (!element.TryGetProperty("output", out JsonElement output) ||
            output.ValueKind != JsonValueKind.Array || output.GetArrayLength() == 0) {
            return null;
        }

        JsonElement result = output[0];
        string item = ReadString(result, "id");
        if (string.IsNullOrEmpty(item)) {
            return null;
        }

        int count = ReadInt(result, "count", 1);
        int data = ReadInt(result, "auxValue", 0);
        return new RecipeResult(item, count, data == 0x7FFF ? 0 : data);
    }

    private static List<string> ResolveTags(JsonElement element) {
        string block = ReadString(element, "block");
        return block switch {
            "crafting_table" => ["crafting_table"],
            "stonecutter" => ["stonecutter"],
            "smithing_table" => ["smithing_table"],
            _ => ["crafting_table"]
        };
    }

    private static FurnaceRecipe? ParseFurnaceRecipe(JsonElement element) {
        string identifier = ReadString(element, "identifier");
        if (string.IsNullOrEmpty(identifier)) return null;

        List<string> tags = ParseStringArray(element, "tags");
        if (tags.Count == 0) return null;

        if (!element.TryGetProperty("input", out JsonElement inputEl) || inputEl.ValueKind != JsonValueKind.Object) {
            return null;
        }

        string inputItem = ReadString(inputEl, "item");
        if (string.IsNullOrEmpty(inputItem)) return null;

        if (!element.TryGetProperty("output", out JsonElement outputEl) || outputEl.ValueKind != JsonValueKind.Object) {
            return null;
        }

        string outputItem = ReadString(outputEl, "item");
        if (string.IsNullOrEmpty(outputItem)) return null;

        return new FurnaceRecipe(identifier, tags, inputItem, outputItem);
    }

    private static CraftingRecipe? ParseRecipe(JsonElement element) {
        string typeStr = ReadString(element, "type");
        RecipeType type = typeStr switch {
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

        switch (type) {
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

    private static Dictionary<char, RecipeIngredient> ParseKey(JsonElement element) {
        Dictionary<char, RecipeIngredient> key = [];
        if (!element.TryGetProperty("key", out JsonElement keyElement) || keyElement.ValueKind != JsonValueKind.Object) {
            return key;
        }

        foreach (JsonProperty property in keyElement.EnumerateObject()) {
            if (property.Name.Length != 1) continue;
            char symbol = property.Name[0];
            RecipeIngredient? ingredient = ParseIngredient(property.Value);
            if (ingredient is not null) {
                key[symbol] = ingredient;
            }
        }

        return key;
    }

    private static List<RecipeIngredient> ParseIngredients(JsonElement element) {
        List<RecipeIngredient> ingredients = [];
        if (!element.TryGetProperty("ingredients", out JsonElement array) || array.ValueKind != JsonValueKind.Array) {
            return ingredients;
        }

        foreach (JsonElement item in array.EnumerateArray()) {
            RecipeIngredient? ingredient = ParseIngredient(item);
            if (ingredient is not null) {
                ingredients.Add(ingredient);
            }
        }

        return ingredients;
    }

    private static RecipeIngredient? ParseIngredient(JsonElement element) {
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

    private static RecipeResult? ParseResult(JsonElement element) {
        if (!element.TryGetProperty("result", out JsonElement resultEl)) return null;

        JsonElement target = resultEl;
        if (resultEl.ValueKind == JsonValueKind.Array) {
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

    private static List<string> ParseStringArray(JsonElement element, string property) {
        List<string> result = [];
        if (!element.TryGetProperty(property, out JsonElement array) || array.ValueKind != JsonValueKind.Array) {
            return result;
        }

        foreach (JsonElement item in array.EnumerateArray()) {
            if (item.ValueKind == JsonValueKind.String) {
                result.Add(item.GetString() ?? string.Empty);
            }
        }

        return result;
    }

    private static string ReadString(JsonElement element, string property) {
        return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
          ? value.GetString() ?? string.Empty
          : string.Empty;
    }

    private static int ReadInt(JsonElement element, string property, int fallback) {
        return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result)
          ? result
          : fallback;
    }

}
