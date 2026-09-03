namespace Basalt.Core.Crafting;

using Basalt.Core.Item;

public sealed class FurnaceRegistry {
    private static FurnaceRegistry? _instance;
    public static FurnaceRegistry Instance => _instance ?? throw new InvalidOperationException("FurnaceRegistry not initialized.");

    private readonly List<FurnaceRecipe> _recipes = [];
    private readonly Dictionary<string, List<FurnaceRecipe>> _byInput = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FurnaceRecipe> _byIdentifier = new(StringComparer.Ordinal);

    public static void Initialize() {
        _instance = new FurnaceRegistry();
    }

    public void Register(FurnaceRecipe recipe) {
        _recipes.Add(recipe);
        _byIdentifier[recipe.Identifier] = recipe;

        if (!_byInput.TryGetValue(recipe.InputItem, out List<FurnaceRecipe>? list)) {
            list = [];
            _byInput[recipe.InputItem] = list;
        }
        list.Add(recipe);
    }

    public FurnaceRecipe? GetRecipe(string inputItem, string tag) {
        if (!_byInput.TryGetValue(inputItem, out List<FurnaceRecipe>? recipes)) {
            return null;
        }

        for (int i = 0; i < recipes.Count; i++) {
            FurnaceRecipe recipe = recipes[i];
            for (int t = 0; t < recipe.Tags.Count; t++) {
                if (string.Equals(recipe.Tags[t], tag, StringComparison.Ordinal)) {
                    return recipe;
                }
            }
        }

        return null;
    }

    public IReadOnlyList<FurnaceRecipe>? GetRecipes(string inputItem) {
        return _byInput.TryGetValue(inputItem, out List<FurnaceRecipe>? recipes) ? recipes : null;
    }

    public FurnaceRecipe? GetByIdentifier(string identifier) {
        return _byIdentifier.TryGetValue(identifier, out FurnaceRecipe? recipe) ? recipe : null;
    }

    public IReadOnlyList<FurnaceRecipe> GetAll() => _recipes;

    public ItemType? ResolveOutput(string inputItem, string tag) {
        FurnaceRecipe? recipe = GetRecipe(inputItem, tag);
        if (recipe is null) return null;

        return ItemType.Get(recipe.OutputItem) ?? ItemType.Get("minecraft:" + recipe.OutputItem);
    }
}
