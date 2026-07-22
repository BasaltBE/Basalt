namespace Basalt.Core.Crafting;

using Basalt.Core.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

public sealed class CraftingRegistry {
    private static CraftingRegistry? _instance;
    public static CraftingRegistry Instance => _instance ?? throw new InvalidOperationException("CraftingRegistry not initialized.");

    private readonly List<CraftingRecipe> _recipes = [];
    private readonly Dictionary<string, int> _identifierToIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, int> _networkIdToIndex = [];
    private byte[]? _cachedPayload;

    public static void Initialize() {
        _instance = new CraftingRegistry();
    }

    public void RegisterShaped(
      string identifier,
      IReadOnlyList<string> pattern,
      IReadOnlyDictionary<char, RecipeIngredient> key,
      RecipeResult result,
      IReadOnlyList<string>? tags = null,
      int priority = 0) {
        CraftingRecipe recipe = new(
          RecipeType.Shaped,
          identifier,
          tags ?? ["crafting_table"],
          priority,
          pattern,
          key,
          Array.Empty<RecipeIngredient>(),
          result);

        AddRecipe(recipe);
    }

    public void RegisterShapeless(
      string identifier,
      IReadOnlyList<RecipeIngredient> ingredients,
      RecipeResult result,
      IReadOnlyList<string>? tags = null,
      int priority = 0) {
        CraftingRecipe recipe = new(
          RecipeType.Shapeless,
          identifier,
          tags ?? ["crafting_table"],
          priority,
          Array.Empty<string>(),
          new Dictionary<char, RecipeIngredient>(),
          ingredients,
          result);

        AddRecipe(recipe);
    }

    public void AddRecipe(CraftingRecipe recipe) {
        _recipes.Add(recipe);
        int index = _recipes.Count - 1;
        _identifierToIndex[recipe.Identifier] = index;
        InvalidateCache();
    }

    public CraftingRecipe? GetByNetworkId(uint networkId) {
        if (_networkIdToIndex.TryGetValue(networkId, out int index) && index < _recipes.Count) {
            return _recipes[index];
        }
        return null;
    }

    public CraftingRecipe? GetByIdentifier(string identifier) {
        if (_identifierToIndex.TryGetValue(identifier, out int index)) {
            return _recipes[index];
        }
        return null;
    }

    public IReadOnlyList<CraftingRecipe> GetAll() => _recipes;

    public byte[] GetCraftingDataPayload() {
        if (_cachedPayload is not null) return _cachedPayload;

        List<CraftingDataEntry> entries = [];
        _networkIdToIndex.Clear();
        uint networkId = 1;

        for (int i = 0; i < _recipes.Count; i++) {
            CraftingRecipe recipe = _recipes[i];
            CraftingDataEntry? entry = BuildEntry(recipe, networkId);
            if (entry is null) {
                networkId++;
                continue;
            }

            entries.Add(entry);
            _networkIdToIndex[networkId] = i;
            networkId++;
        }

        int furnaceSkipped = 0;
        IReadOnlyList<FurnaceRecipe> furnaceRecipes = FurnaceRegistry.Instance.GetAll();
        for (int i = 0; i < furnaceRecipes.Count; i++) {
            FurnaceRecipe furnace = furnaceRecipes[i];
            for (int t = 0; t < furnace.Tags.Count; t++) {
                CraftingDataEntry? entry = BuildFurnaceAsShapeless(furnace, furnace.Tags[t], networkId);
                if (entry is null) {
                    furnaceSkipped++;
                    networkId++;
                    continue;
                }

                entries.Add(entry);
                networkId++;
            }
        }

        CraftingDataPacket packet = new() {
            Recipes = entries,
            ClearRecipes = true
        };

        _cachedPayload = SerializePacket(packet);
        return _cachedPayload;
    }

    private void InvalidateCache() {
        _cachedPayload = null;
        _networkIdToIndex.Clear();
    }

    private static CraftingDataEntry? BuildEntry(CraftingRecipe recipe, uint networkId) {
        // if(networkId == 248)
        // {
        //   Logger.Info("Item {0}", recipe.Identifier);
        // };

        // num + 1 // breaks at id 248
        // if (networkId > 246)
        // {
        //   for (int t = 0; t < recipe.Tags.Count; t++)
        //   {
        //     if (recipe.Tags[t] == "crafting_table") return null;
        //   }
        // }

        ItemType? resultType = ResolveItemType(recipe.Result.Item);
        if (resultType is null) {
            Logger.Warn("Crafting: skipping '{0}', result item '{1}' not found.", recipe.Identifier, recipe.Result.Item);
            return null;
        }

        byte[] uuid = Guid.NewGuid().ToByteArray();

        return recipe.Type switch {
            RecipeType.Shaped => BuildShapedEntry(recipe, resultType, networkId, uuid),
            RecipeType.Shapeless => BuildShapelessEntry(recipe, resultType, networkId, uuid),
            _ => null
        };
    }

    private static CraftingDataEntry? BuildShapedEntry(CraftingRecipe recipe, ItemType resultType, uint networkId, byte[] uuid) {
        if (recipe.Pattern.Count == 0) return null;

        int height = recipe.Pattern.Count;
        int width = recipe.Pattern[0].Length;

        List<ItemDescriptorCount> input = new(width * height);
        for (int row = 0; row < height; row++) {
            string line = recipe.Pattern[row];
            for (int col = 0; col < width; col++) {
                char symbol = col < line.Length ? line[col] : ' ';
                if (symbol == ' ') {
                    input.Add(new ItemDescriptorCount { DescriptorType = 0, Count = 0 });
                    continue;
                }

                if (!recipe.Key.TryGetValue(symbol, out RecipeIngredient? ingredient)) {
                    input.Add(new ItemDescriptorCount { DescriptorType = 0, Count = 0 });
                    continue;
                }

                ItemDescriptorCount? descriptor = BuildDescriptor(ingredient);
                if (descriptor is null) {
                    Logger.Warn("Crafting: skipping '{0}', ingredient for '{1}' not found.", recipe.Identifier, symbol);
                    return null;
                }
                input.Add(descriptor);
            }
        }

        ShapedRecipeData data = new() {
            RecipeId = recipe.Identifier,
            Width = width,
            Height = height,
            Input = input,
            Output = [BuildResultItem(resultType, recipe.Result)],
            Uuid = uuid,
            Block = ResolveBlock(recipe.Tags),
            Priority = recipe.Priority,
            AssumeSymmetry = true,
            UnlockRequirement = new RecipeUnlockingRequirement { Context = RecipeUnlockingRequirement.ContextNone },
            RecipeNetworkId = networkId
        };

        return new CraftingDataEntry {
            RecipeType = CraftingDataRecipeType.Shaped,
            Shaped = data
        };
    }

    private static CraftingDataEntry? BuildShapelessEntry(CraftingRecipe recipe, ItemType resultType, uint networkId, byte[] uuid) {
        List<ItemDescriptorCount> input = new(recipe.Ingredients.Count);
        for (int i = 0; i < recipe.Ingredients.Count; i++) {
            ItemDescriptorCount? descriptor = BuildDescriptor(recipe.Ingredients[i]);
            if (descriptor is null) {
                Logger.Warn("Crafting: skipping '{0}', ingredient '{1}' not found.", recipe.Identifier, recipe.Ingredients[i].Item ?? recipe.Ingredients[i].Tag);
                return null;
            }
            input.Add(descriptor);
        }

        ShapelessRecipeData data = new() {
            RecipeId = recipe.Identifier,
            Input = input,
            Output = [BuildResultItem(resultType, recipe.Result)],
            Uuid = uuid,
            Block = ResolveBlock(recipe.Tags),
            Priority = recipe.Priority,
            UnlockRequirement = new RecipeUnlockingRequirement { Context = RecipeUnlockingRequirement.ContextNone },
            RecipeNetworkId = networkId
        };

        return new CraftingDataEntry {
            RecipeType = CraftingDataRecipeType.Shapeless,
            Shapeless = data
        };
    }

    private static CraftingDataEntry? BuildFurnaceAsShapeless(FurnaceRecipe recipe, string block, uint networkId) {
        ItemType? inputType = ResolveItemType(recipe.InputItem);
        if (inputType is null) {
            Logger.Warn("Crafting: skipping furnace '{0}', input '{1}' not found.", recipe.Identifier, recipe.InputItem);
            return null;
        }

        ItemType? outputType = ResolveItemType(recipe.OutputItem);
        if (outputType is null) {
            Logger.Warn("Crafting: skipping furnace '{0}', output '{1}' not found.", recipe.Identifier, recipe.OutputItem);
            return null;
        }

        ItemDescriptorCount inputDescriptor = new() {
            DescriptorType = 1,
            NetworkId = checked((short)inputType.NetworkId),
            MetadataValue = 0x7FFF,
            Count = 1
        };

        int blockRuntimeId = 0;
        if (outputType.BlockType is not null && outputType.BlockType.Permutations.Count > 0) {
            blockRuntimeId = outputType.BlockType.Permutations[0].NetworkId;
        }

        RecipeItemStack output = new() {
            NetworkId = outputType.NetworkId,
            Count = 1,
            Metadata = 0,
            BlockRuntimeId = blockRuntimeId
        };

        ShapelessRecipeData data = new() {
            RecipeId = recipe.Identifier,
            Input = [inputDescriptor],
            Output = [output],
            Uuid = Guid.NewGuid().ToByteArray(),
            Block = block,
            Priority = 0,
            UnlockRequirement = new RecipeUnlockingRequirement {
                Context = RecipeUnlockingRequirement.ContextNone,
                Ingredients = [inputDescriptor]
            },
            RecipeNetworkId = networkId
        };

        return new CraftingDataEntry {
            RecipeType = CraftingDataRecipeType.Shapeless,
            Shapeless = data
        };
    }

    private static ItemDescriptorCount? BuildDescriptor(RecipeIngredient ingredient) {
        if (ingredient.Tag is not null) {
            return new ItemDescriptorCount {
                DescriptorType = 3,
                Text = ingredient.Tag,
                Count = ingredient.Count
            };
        }

        if (ingredient.Item is null) return null;

        ItemType? type = ResolveItemType(ingredient.Item);
        if (type is null) return null;

        return new ItemDescriptorCount {
            DescriptorType = 1,
            NetworkId = checked((short)type.NetworkId),
            MetadataValue = 0x7FFF,
            Count = ingredient.Count
        };
    }

    private static RecipeItemStack BuildResultItem(ItemType type, RecipeResult result) {
        int blockRuntimeId = 0;
        if (type.BlockType is not null && type.BlockType.Permutations.Count > 0) {
            blockRuntimeId = type.BlockType.Permutations[0].NetworkId;
        }

        return new RecipeItemStack {
            NetworkId = type.NetworkId,
            Count = checked((ushort)result.Count),
            Metadata = unchecked((uint)result.Data),
            BlockRuntimeId = blockRuntimeId
        };
    }

    private static ItemType? ResolveItemType(string identifier) {
        ItemType? type = ItemType.Get(identifier);
        if (type is not null) return type;

        if (!identifier.Contains(':')) {
            type = ItemType.Get("minecraft:" + identifier);
        }

        return type;
    }

    private static string ResolveBlock(IReadOnlyList<string> tags) {
        for (int i = 0; i < tags.Count; i++) {
            switch (tags[i]) {
                case "crafting_table": return "crafting_table";
                case "stonecutter": return "stonecutter";
                case "furnace": return "furnace";
                case "blast_furnace": return "blast_furnace";
                case "smoker": return "smoker";
                case "campfire": return "campfire";
                case "cartography_table": return "cartography_table";
                case "smithing_table": return "smithing_table";
            }
        }
        return "crafting_table";
    }

    private static byte[] SerializePacket(DataPacket packet) {
        int size = 1024 * 256;
        while (true) {
            byte[] buffer = new byte[size];
            try {
                int offset = 0;
                BinaryWriter writer = new(buffer, ref offset);
                packet.Serialize(writer);
                return writer.GetProcessedBytes().ToArray();
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException) {
                size *= 2;
            }
        }
    }
}
