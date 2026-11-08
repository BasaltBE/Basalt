using System.Buffers.Binary;
using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Crafting;

internal static class CraftingPalette {
    private static readonly Dictionary<int, CraftingRecipe> recipes;
    internal static readonly CraftingDataPacket Packet;

    static CraftingPalette() {
        Packet = CreatePacket(out recipes);
    }

    internal static CraftingRecipe? FindRecipe(int netId) => recipes.GetValueOrDefault(netId);

    static CraftingDataPacket CreatePacket(out Dictionary<int, CraftingRecipe> recipeMap) {
        Assembly assembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = assembly.GetManifestResourceStream("BasaltBE.Data.Data.recipes.json")
            ?? throw new InvalidOperationException("Recipe data file 'recipes.json' is missing.");
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement recipes = document.RootElement.GetProperty("recipes");
        List<ShapedRecipePayload> shaped = [];
        List<ShapelessRecipePayload> shapeless = [];
        recipeMap = [];
        uint netId = 1;

        foreach (JsonElement recipe in recipes.EnumerateArray()) {
            int type = ReadInt(recipe, "type", -1);
            bool isShaped = type == 1;
            if (!isShaped && type is not (0 or 5)) continue;

            string identifier = ReadString(recipe, "id");
            if (identifier.Length == 0 || !recipe.TryGetProperty("output", out JsonElement outputs) ||
                outputs.ValueKind != JsonValueKind.Array || outputs.GetArrayLength() == 0)
                continue;

            JsonElement output = outputs[0];
            ItemType? result = FindItem(ReadString(output, "id"));
            if (result is null) continue;

            ushort resultCount = checked((ushort)Math.Clamp(ReadInt(output, "count", 1), 1, ushort.MaxValue));
            uint resultMetadata = unchecked((uint)ReadInt(output, "auxValue", 0));
            NetworkItemInstanceDescriptor resultItem = new() {
                Id = result.NetworkId,
                StackSize = resultCount,
                AuxValue = resultMetadata
            };
            string tag = ResolveTag(ReadString(recipe, "block"), identifier);
            int priority = ReadInt(recipe, "priority", 0);
            Uuid uuid = CreateUuid(Guid.NewGuid());
            int recipeNetId = checked((int)netId);

            if (isShaped) {
                if (!recipe.TryGetProperty("input", out JsonElement input) || input.ValueKind != JsonValueKind.Object ||
                    !recipe.TryGetProperty("shape", out JsonElement shape) || shape.ValueKind != JsonValueKind.Array)
                    continue;

                string[] rows = shape.EnumerateArray().Select(value => value.GetString() ?? string.Empty).ToArray();
                if (rows.Length == 0) continue;

                Dictionary<char, RecipeIngredient> key = [];
                foreach (JsonProperty property in input.EnumerateObject()) {
                    if (property.Name.Length != 1) continue;

                    RecipeIngredient? ingredient = CreateIngredient(property.Value);
                    if (ingredient is not null) key[property.Name[0]] = ingredient;
                }

                int width = rows.Max(row => row.Length);
                List<RecipeIngredient> ingredients = new(rows.Length * width);
                bool valid = true;
                for (int row = 0; row < rows.Length && valid; row++) {
                    for (int column = 0; column < width; column++) {
                        char symbol = column < rows[row].Length ? rows[row][column] : ' ';
                        if (symbol == ' ') {
                            ingredients.Add(new RecipeIngredient { StackSize = 1 });
                            continue;
                        }
                        if (!key.TryGetValue(symbol, out RecipeIngredient? ingredient)) {
                            valid = false;
                            break;
                        }

                        ingredients.Add(ingredient);
                    }
                }

                if (!valid) continue;

                recipeMap.Add(recipeNetId, new CraftingRecipe(
                    tag,
                    width,
                    rows.Length,
                    ingredients.ToArray(),
                    result,
                    resultCount,
                    resultMetadata,
                    ReadBool(recipe, "assumeSymetry", true)
                ));
                shaped.Add(new ShapedRecipePayload {
                    RecipeId = identifier,
                    Width = width,
                    Height = rows.Length,
                    Ingredients = ingredients.ToArray(),
                    Results = [resultItem],
                    Uuid = uuid,
                    Tag = tag,
                    Priority = priority,
                    AssumeSymmetry = ReadBool(recipe, "assumeSymetry", true),
                    UnlockingRequirement = new RecipeUnlockingRequirement { UnlockingContext = 1 },
                    NetId = netId++
                });
                continue;
            }

            if (!recipe.TryGetProperty("input", out JsonElement inputs) || inputs.ValueKind != JsonValueKind.Array) continue;

            List<RecipeIngredient> shapelessIngredients = [];
            foreach (JsonElement input in inputs.EnumerateArray()) {
                RecipeIngredient? ingredient = CreateIngredient(input);
                if (ingredient is null) {
                    shapelessIngredients.Clear();
                    break;
                }

                shapelessIngredients.Add(ingredient);
            }

            if (shapelessIngredients.Count == 0) continue;

            recipeMap.Add(recipeNetId, new CraftingRecipe(
                tag,
                0,
                0,
                shapelessIngredients.ToArray(),
                result,
                resultCount,
                resultMetadata,
                false
            ));
            shapeless.Add(new ShapelessRecipePayload {
                RecipeId = identifier,
                Ingredients = shapelessIngredients.ToArray(),
                Results = [resultItem],
                Uuid = uuid,
                Tag = tag,
                Priority = priority,
                UnlockingRequirement = new RecipeUnlockingRequirement { UnlockingContext = 1 },
                NetId = netId++
            });
        }

        return new CraftingDataPacket {
            ShapedRecipes = shaped.ToArray(),
            ShapelessRecipes = shapeless.ToArray(),
            ClearRecipes = true
        };
    }

    static RecipeIngredient? CreateIngredient(JsonElement value) {
        string item = ReadString(value, "id");
        string tag = ReadString(value, "itemTag");
        int count = Math.Clamp(ReadInt(value, "count", 1), 1, 64);
        if (tag.Length > 0)
            return new RecipeIngredient { Descriptor = "item_tag", DescriptorValue = tag, AuxValue = 0x7FFF, StackSize = count };
        ItemType? type = FindItem(item);
        return type is null
            ? null
            : new RecipeIngredient {
                Descriptor = "name",
                DescriptorValue = type.Identifier,
                AuxValue = ReadInt(value, "auxValue", 0x7FFF),
                StackSize = count
            };
    }

    static ItemType? FindItem(string identifier) {
        ItemType? type = ItemPalette.Find(identifier);
        return type ?? (identifier.Contains(':') ? null : ItemPalette.Find("minecraft:" + identifier));
    }

    static string ResolveTag(string block, string identifier) => block switch {
        "stonecutter" or "furnace" or "blast_furnace" or "smoker" or "campfire" or "smithing_table" => block,
        "deprecated" when identifier.StartsWith("minecraft:furnace_", StringComparison.Ordinal) => "furnace",
        _ => "crafting_table"
    };

    static Uuid CreateUuid(Guid value) {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);
        return new Uuid {
            MostSignificantBits = BinaryPrimitives.ReadUInt64LittleEndian(bytes[..8]),
            LeastSignificantBits = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..])
        };
    }

    static int ReadInt(JsonElement element, string property, int fallback) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result)
            ? result
            : fallback;

    static string ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    static bool ReadBool(JsonElement element, string property, bool fallback) =>
        element.TryGetProperty(property, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;
}
