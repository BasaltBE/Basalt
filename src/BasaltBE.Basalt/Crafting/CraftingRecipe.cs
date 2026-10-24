using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Crafting;

internal sealed class CraftingRecipe(
    string tag,
    int width,
    int height,
    RecipeIngredient[] ingredients,
    ItemType result,
    ushort resultCount,
    uint resultMetadata,
    bool assumeSymmetry
) {
    internal readonly string Tag = tag;
    internal readonly int Width = width;
    internal readonly int Height = height;
    internal readonly RecipeIngredient[] Ingredients = ingredients;
    internal readonly ItemType Result = result;
    internal readonly ushort ResultCount = resultCount;
    internal readonly uint ResultMetadata = resultMetadata;
    internal readonly bool AssumeSymmetry = assumeSymmetry;
    internal bool Shaped => Width > 0;

    internal bool TryMatch(ItemStack?[] grid, int gridWidth, int crafts, Span<int> consumption) {
        if (crafts <= 0 || grid.Length != gridWidth * gridWidth || consumption.Length < grid.Length ||
            Shaped && (Width > gridWidth || Height > gridWidth))
            return false;

        consumption[..grid.Length].Clear();
        return Shaped
            ? MatchShaped(grid, gridWidth, crafts, consumption)
            : MatchShapeless(grid, crafts, consumption);
    }

    internal bool TryMatchInventory(ItemStack?[] inventory, int crafts, Span<int> consumption) {
        if (crafts <= 0 || consumption.Length < inventory.Length)
            return false;

        consumption[..inventory.Length].Clear();
        foreach (RecipeIngredient ingredient in Ingredients) {
            if (ingredient.Descriptor.Length == 0)
                continue;

            int remaining = ingredient.StackSize * crafts;
            for (int slot = 0; slot < inventory.Length && remaining > 0; slot++) {
                ItemStack? item = inventory[slot];
                if (item is null || !Matches(ingredient, item))
                    continue;

                int available = item.StackSize - consumption[slot];
                int consumed = Math.Min(available, remaining);
                consumption[slot] += consumed;
                remaining -= consumed;
            }

            if (remaining > 0)
                return false;
        }

        return true;
    }

    private bool MatchShaped(ItemStack?[] grid, int gridWidth, int crafts, Span<int> consumption) {
        for (int top = 0; top <= gridWidth - Height; top++) {
            for (int left = 0; left <= gridWidth - Width; left++) {
                if (MatchShapedAt(grid, gridWidth, top, left, crafts, consumption, mirror: false))
                    return true;
                if (AssumeSymmetry && MatchShapedAt(grid, gridWidth, top, left, crafts, consumption, mirror: true))
                    return true;
            }
        }
        return false;
    }

    private bool MatchShapedAt(
        ItemStack?[] grid,
        int gridWidth,
        int top,
        int left,
        int crafts,
        Span<int> consumption,
        bool mirror
    ) {
        consumption[..grid.Length].Clear();
        for (int y = 0; y < gridWidth; y++) {
            for (int x = 0; x < gridWidth; x++) {
                int gridIndex = y * gridWidth + x;
                int recipeX = x - left;
                int recipeY = y - top;
                if ((uint)recipeX >= Width || (uint)recipeY >= Height) {
                    if (grid[gridIndex] is not null)
                        return false;
                    continue;
                }

                if (mirror)
                    recipeX = Width - recipeX - 1;
                RecipeIngredient ingredient = Ingredients[recipeY * Width + recipeX];
                ItemStack? item = grid[gridIndex];
                if (ingredient.Descriptor.Length == 0) {
                    if (item is not null)
                        return false;
                    continue;
                }

                int amount = ingredient.StackSize * crafts;
                if (item is null || !Matches(ingredient, item) || item.StackSize < amount)
                    return false;
                consumption[gridIndex] = amount;
            }
        }
        return true;
    }

    private bool MatchShapeless(ItemStack?[] grid, int crafts, Span<int> consumption) {
        if (Ingredients.Length > grid.Length)
            return false;

        int occupied = 0;
        for (int slot = 0; slot < grid.Length; slot++) {
            if (grid[slot] is not null)
                occupied |= 1 << slot;
        }

        Span<byte> memo = stackalloc byte[(Ingredients.Length + 1) * 512];
        return MatchShapelessAt(grid, crafts, consumption, 0, 0, occupied, memo);
    }

    private bool MatchShapelessAt(
        ItemStack?[] grid,
        int crafts,
        Span<int> consumption,
        int ingredientIndex,
        int usedSlots,
        int occupiedSlots,
        Span<byte> memo
    ) {
        int memoIndex = ingredientIndex * 512 + usedSlots;
        if (memo[memoIndex] == 2)
            return false;
        if (ingredientIndex == Ingredients.Length) {
            bool complete = usedSlots == occupiedSlots;
            if (!complete)
                memo[memoIndex] = 2;
            return complete;
        }

        RecipeIngredient ingredient = Ingredients[ingredientIndex];
        int amount = ingredient.StackSize * crafts;
        for (int slot = 0; slot < grid.Length; slot++) {
            int bit = 1 << slot;
            ItemStack? item = grid[slot];
            if ((occupiedSlots & bit) == 0 || (usedSlots & bit) != 0 || item is null ||
                item.StackSize < amount || !Matches(ingredient, item))
                continue;

            consumption[slot] = amount;
            if (MatchShapelessAt(grid, crafts, consumption, ingredientIndex + 1, usedSlots | bit, occupiedSlots, memo))
                return true;
            consumption[slot] = 0;
        }

        memo[memoIndex] = 2;
        return false;
    }

    private static bool Matches(RecipeIngredient ingredient, ItemStack stack) {
        ItemType item = stack.Type;
        if (ingredient.Descriptor == "name") {
            bool legacyLog = item.Identifier is "minecraft:log" or "minecraft:log2";
            string identifier = legacyLog
                ? (item.Identifier, stack.Metadata) switch {
                    ("minecraft:log", 0) => "minecraft:oak_log",
                    ("minecraft:log", 1) => "minecraft:spruce_log",
                    ("minecraft:log", 2) => "minecraft:birch_log",
                    ("minecraft:log", 3) => "minecraft:jungle_log",
                    ("minecraft:log2", 0) => "minecraft:acacia_log",
                    ("minecraft:log2", 1) => "minecraft:dark_oak_log",
                    _ => string.Empty
                }
                : item.Identifier;
            return identifier == ingredient.DescriptorValue &&
                (ingredient.AuxValue is 0x7FFF or -1 ||
                    (legacyLog ? ingredient.AuxValue == 0 : stack.Metadata == unchecked((uint)ingredient.AuxValue)));
        }
        if (ingredient.Descriptor != "item_tag")
            return false;

        foreach (string tag in item.Tags) {
            if (tag == ingredient.DescriptorValue)
                return true;
        }
        foreach (string tag in item.PropertiesPayload.ItemTags) {
            if (tag == ingredient.DescriptorValue)
                return true;
        }
        return false;
    }
}
