using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Crafting;

internal static class CookingPalette {
    private static readonly Dictionary<(string Input, string Furnace), CookingRecipe> recipes = [];
    private static readonly Dictionary<string, int> fuelTimes = new(StringComparer.Ordinal) {
        ["minecraft:coal"] = 1600,
        ["minecraft:charcoal"] = 1600,
        ["minecraft:coal_block"] = 16000,
        ["minecraft:lava_bucket"] = 20000,
        ["minecraft:blaze_rod"] = 2400,
        ["minecraft:dried_kelp_block"] = 4001,
        ["minecraft:bamboo"] = 50,
        ["minecraft:stick"] = 100,
        ["minecraft:bow"] = 300,
        ["minecraft:fishing_rod"] = 300,
        ["minecraft:crafting_table"] = 300,
        ["minecraft:chest"] = 300,
        ["minecraft:barrel"] = 300,
        ["minecraft:bookshelf"] = 300,
        ["minecraft:ladder"] = 300,
        ["minecraft:wooden_sword"] = 300,
        ["minecraft:wooden_pickaxe"] = 300,
        ["minecraft:wooden_axe"] = 300,
        ["minecraft:wooden_shovel"] = 300,
        ["minecraft:wooden_hoe"] = 300,
        ["minecraft:white_wool"] = 100,
        ["minecraft:white_carpet"] = 67
    };

    internal static void Initialize() {
        LoadRecipes();
        LoadFuelTimes();
    }

    internal static CookingRecipe? Find(ItemStack? input, string furnace) =>
        input is not null && recipes.TryGetValue((input.Type.Identifier, furnace), out CookingRecipe recipe)
            ? recipe
            : null;

    internal static int GetBurnTime(ItemStack? fuel) =>
        fuel is null ? 0 : fuel.Type.FuelDurationTicks > 0 ? fuel.Type.FuelDurationTicks :
            fuelTimes.GetValueOrDefault(fuel.Type.Identifier);

    private static void LoadRecipes() {
        recipes.Clear();
        Assembly assembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = assembly.GetManifestResourceStream("BasaltBE.Data.Data.recipes.json")
            ?? throw new InvalidOperationException("Recipe data file 'recipes.json' is missing.");
        using JsonDocument document = JsonDocument.Parse(stream);

        foreach (JsonElement recipe in document.RootElement.GetProperty("recipes").EnumerateArray()) {
            if (!recipe.TryGetProperty("type", out JsonElement typeValue) ||
                typeValue.ValueKind != JsonValueKind.Number || !typeValue.TryGetInt32(out int type) || type != 0 ||
                !recipe.TryGetProperty("block", out JsonElement blockValue) || blockValue.ValueKind != JsonValueKind.String)
                continue;

            if (!recipe.TryGetProperty("input", out JsonElement inputs) ||
                inputs.ValueKind != JsonValueKind.Array || inputs.GetArrayLength() == 0 ||
                !recipe.TryGetProperty("output", out JsonElement outputs) ||
                outputs.ValueKind != JsonValueKind.Array || outputs.GetArrayLength() == 0)
                continue;

            string furnace = blockValue.GetString() ?? string.Empty;
            if (furnace == "deprecated") {
                if (!recipe.TryGetProperty("id", out JsonElement idValue) ||
                    idValue.ValueKind != JsonValueKind.String ||
                    !(idValue.GetString() ?? string.Empty).StartsWith("minecraft:furnace_", StringComparison.Ordinal))
                    continue;

                furnace = "furnace";
            }

            if (furnace is not ("furnace" or "blast_furnace" or "smoker")) continue;

            JsonElement input = inputs[0];
            JsonElement output = outputs[0];
            if (!input.TryGetProperty("id", out JsonElement inputId) || inputId.ValueKind != JsonValueKind.String ||
                !output.TryGetProperty("id", out JsonElement outputId) || outputId.ValueKind != JsonValueKind.String)
                continue;

            string inputIdentifier = inputId.GetString() ?? string.Empty;
            ItemType? result = ItemPalette.Find(outputId.GetString() ?? string.Empty);
            if (inputIdentifier.Length == 0 || result is null) continue;

            int count = output.TryGetProperty("count", out JsonElement countValue) &&
                countValue.ValueKind == JsonValueKind.Number && countValue.TryGetInt32(out int parsedCount)
                ? parsedCount
                : 1;
            int metadata = output.TryGetProperty("auxValue", out JsonElement metadataValue) &&
                metadataValue.ValueKind == JsonValueKind.Number && metadataValue.TryGetInt32(out int parsedMetadata)
                ? parsedMetadata
                : 0;

            recipes.TryAdd(
                (inputIdentifier, furnace),
                new CookingRecipe(result, checked((ushort)Math.Clamp(count, 1, ushort.MaxValue)), unchecked((uint)metadata))
            );
        }
    }

    private static void LoadFuelTimes() {
        foreach (ItemType item in ItemPalette.Types) {
            string identifier = item.Identifier;
            if (Array.IndexOf(item.Tags, "minecraft:planks") >= 0 ||
                Array.IndexOf(item.Tags, "minecraft:logs_that_burn") >= 0 ||
                Array.IndexOf(item.Tags, "minecraft:crimson_stems") >= 0 ||
                Array.IndexOf(item.Tags, "minecraft:warped_stems") >= 0 ||
                identifier.EndsWith("_log", StringComparison.Ordinal) ||
                identifier.EndsWith("_wood", StringComparison.Ordinal) ||
                identifier.EndsWith("_stem", StringComparison.Ordinal) ||
                identifier.EndsWith("_hyphae", StringComparison.Ordinal) ||
                identifier.EndsWith("_boat", StringComparison.Ordinal)) {
                fuelTimes.TryAdd(identifier, 300);
            } else if (Array.IndexOf(item.Tags, "minecraft:wooden_slabs") >= 0) {
                fuelTimes.TryAdd(identifier, 150);
            } else if (Array.IndexOf(item.Tags, "minecraft:wool") >= 0 ||
                identifier.EndsWith("_sapling", StringComparison.Ordinal) ||
                identifier.EndsWith("_sign", StringComparison.Ordinal)) {
                fuelTimes.TryAdd(identifier, 100);
            } else if (identifier.EndsWith("_carpet", StringComparison.Ordinal)) {
                fuelTimes.TryAdd(identifier, 67);
            }
        }
    }
}

internal readonly record struct CookingRecipe(ItemType Result, ushort Count, uint Metadata);
