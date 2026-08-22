namespace Basalt.Core.Crafting;

using System.Buffers.Binary;
using Basalt.Core.Item;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using ProtocolRecipeIngredient = Basalt.BedrockProtocol.Types.RecipeIngredient;

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
        if (_cachedPayload is not null) {
            return _cachedPayload;
        }

        List<ShapedRecipePayload> shapedRecipes = [];
        List<ShapelessRecipePayload> shapelessRecipes = [];

        _networkIdToIndex.Clear();

        uint networkId = 1;

        for (int i = 0; i < _recipes.Count; i++) {
            CraftingRecipe recipe = _recipes[i];

            switch (recipe.Type) {
                case RecipeType.Shaped: {
                        ShapedRecipePayload? payload = BuildShapedRecipe(recipe, networkId);
                        if (payload is not null) {
                            shapedRecipes.Add(payload);
                            _networkIdToIndex[networkId] = i;
                        }

                        break;
                    }

                case RecipeType.Shapeless: {
                        ShapelessRecipePayload? payload = BuildShapelessRecipe(recipe, networkId);
                        if (payload is not null) {
                            shapelessRecipes.Add(payload);
                            _networkIdToIndex[networkId] = i;
                        }

                        break;
                    }
            }

            networkId++;
        }

        // Keep the old behaviour where furnace recipes were exposed to the
        // client as shapeless recipes.
        IReadOnlyList<FurnaceRecipe> furnaceRecipes = FurnaceRegistry.Instance.GetAll();

        for (int i = 0; i < furnaceRecipes.Count; i++) {
            FurnaceRecipe furnace = furnaceRecipes[i];

            for (int t = 0; t < furnace.Tags.Count; t++) {
                ShapelessRecipePayload? payload =
                    BuildFurnaceAsShapeless(furnace, furnace.Tags[t], networkId);

                if (payload is not null) {
                    shapelessRecipes.Add(payload);
                }

                networkId++;
            }
        }

        CraftingDataPacket packet = new() {
            ShapedRecipes = shapedRecipes.ToArray(),
            ShapelessRecipes = shapelessRecipes.ToArray(),

            MultiRecipes = [],
            UserDataShapelessRecipes = [],
            ShapelessChemistryRecipes = [],
            ShapedChemistryRecipes = [],
            SmithingTransformRecipes = [],
            SmithingTrimRecipes = [],
            PotionMixes = [],
            ContainerMixes = [],
            MaterialReducers = [],

            ClearRecipes = true
        };

        _cachedPayload = SerializePacket(packet);
        return _cachedPayload;
    }

    private void InvalidateCache() {
        _cachedPayload = null;
        _networkIdToIndex.Clear();
    }

    private static ShapedRecipePayload? BuildShapedRecipe(
        CraftingRecipe recipe,
        uint networkId) {

        if (recipe.Pattern.Count == 0) {
            return null;
        }

        ItemType? resultType = ResolveItemType(recipe.Result.Item);
        if (resultType is null) {
            Logger.Warn(
                "Crafting: skipping '{0}', result item '{1}' not found.",
                recipe.Identifier,
                recipe.Result.Item);

            return null;
        }

        int height = recipe.Pattern.Count;
        int width = recipe.Pattern[0].Length;

        List<ProtocolRecipeIngredient> ingredients = new(width * height);

        for (int row = 0; row < height; row++) {
            string line = recipe.Pattern[row];

            for (int col = 0; col < width; col++) {
                char symbol = col < line.Length
                    ? line[col]
                    : ' ';

                if (symbol == ' ') {
                    ingredients.Add(CreateEmptyIngredient());
                    continue;
                }

                if (!recipe.Key.TryGetValue(symbol, out RecipeIngredient? ingredient)) {
                    ingredients.Add(CreateEmptyIngredient());
                    continue;
                }

                ProtocolRecipeIngredient? descriptor = BuildDescriptor(ingredient);

                if (descriptor is null) {
                    Logger.Warn(
                        "Crafting: skipping '{0}', ingredient for '{1}' not found.",
                        recipe.Identifier,
                        symbol);

                    return null;
                }

                ingredients.Add(descriptor);
            }
        }

        return new ShapedRecipePayload {
            RecipeId = recipe.Identifier,
            Width = width,
            Height = height,
            Ingredients = ingredients.ToArray(),
            Results = [BuildResultItem(resultType, recipe.Result)],
            Uuid = CreateProtocolUuid(Guid.NewGuid()),
            Tag = ResolveBlock(recipe.Tags),
            Priority = recipe.Priority,
            AssumeSymmetry = true,

            UnlockingRequirement = new RecipeUnlockingRequirement {
                UnlockingContext = 1,
                UnlockingIngredients = null
            },

            NetId = networkId
        };
    }

    private static ShapelessRecipePayload? BuildShapelessRecipe(
        CraftingRecipe recipe,
        uint networkId) {

        ItemType? resultType = ResolveItemType(recipe.Result.Item);
        if (resultType is null) {
            Logger.Warn(
                "Crafting: skipping '{0}', result item '{1}' not found.",
                recipe.Identifier,
                recipe.Result.Item);

            return null;
        }

        List<ProtocolRecipeIngredient> ingredients =
            new(recipe.Ingredients.Count);

        for (int i = 0; i < recipe.Ingredients.Count; i++) {
            ProtocolRecipeIngredient? descriptor =
                BuildDescriptor(recipe.Ingredients[i]);

            if (descriptor is null) {
                Logger.Warn(
                    "Crafting: skipping '{0}', ingredient '{1}' not found.",
                    recipe.Identifier,
                    recipe.Ingredients[i].Item ?? recipe.Ingredients[i].Tag);

                return null;
            }

            ingredients.Add(descriptor);
        }

        return new ShapelessRecipePayload {
            RecipeId = recipe.Identifier,
            Ingredients = ingredients.ToArray(),
            Results = [BuildResultItem(resultType, recipe.Result)],
            Uuid = CreateProtocolUuid(Guid.NewGuid()),
            Tag = ResolveBlock(recipe.Tags),
            Priority = recipe.Priority,

            UnlockingRequirement = new RecipeUnlockingRequirement {
                UnlockingContext = 1,
                UnlockingIngredients = null
            },

            NetId = networkId
        };
    }

    private static ShapelessRecipePayload? BuildFurnaceAsShapeless(
        FurnaceRecipe recipe,
        string block,
        uint networkId) {

        ItemType? inputType = ResolveItemType(recipe.InputItem);
        if (inputType is null) {
            Logger.Warn(
                "Crafting: skipping furnace '{0}', input '{1}' not found.",
                recipe.Identifier,
                recipe.InputItem);

            return null;
        }

        ItemType? outputType = ResolveItemType(recipe.OutputItem);
        if (outputType is null) {
            Logger.Warn(
                "Crafting: skipping furnace '{0}', output '{1}' not found.",
                recipe.Identifier,
                recipe.OutputItem);

            return null;
        }

        ProtocolRecipeIngredient input = new() {
            Descriptor = "name",
            DescriptorValue = inputType.Identifier,
            AuxValue = 0x7FFF,
            StackSize = 1
        };

        RecipeResult result = new(
            recipe.OutputItem,
            1,
            0);

        return new ShapelessRecipePayload {
            RecipeId = recipe.Identifier,
            Ingredients = [input],
            Results = [BuildResultItem(outputType, result)],
            Uuid = CreateProtocolUuid(Guid.NewGuid()),
            Tag = block,
            Priority = 0,

            UnlockingRequirement = new RecipeUnlockingRequirement {
                UnlockingContext = 0,
                UnlockingIngredients = [input]
            },

            NetId = networkId
        };
    }

    private static ProtocolRecipeIngredient? BuildDescriptor(
        RecipeIngredient ingredient) {

        int stackSize = Math.Clamp(ingredient.Count, 1, 64);

        if (ingredient.Tag is not null) {
            return new ProtocolRecipeIngredient {
                Descriptor = "item_tag",
                DescriptorValue = ingredient.Tag,
                AuxValue = 0x7FFF,
                StackSize = stackSize
            };
        }

        if (ingredient.Item is null) {
            return null;
        }

        ItemType? type = ResolveItemType(ingredient.Item);
        if (type is null) {
            return null;
        }

        return new ProtocolRecipeIngredient {
            Descriptor = "name",
            DescriptorValue = type.Identifier,
            AuxValue = 0x7FFF,
            StackSize = stackSize
        };
    }

    private static ProtocolRecipeIngredient CreateEmptyIngredient() {
        return new ProtocolRecipeIngredient {
            Descriptor = string.Empty,
            DescriptorValue = string.Empty,
            AuxValue = 0x7FFF,
            StackSize = 1
        };
    }

    private static NetworkItemInstanceDescriptor BuildResultItem(
        ItemType type,
        RecipeResult result) {

        int blockRuntimeId = 0;

        if (
            type.BlockType is not null
            && type.BlockType.Permutations.Count > 0
        ) {
            blockRuntimeId = type.BlockType.Permutations[0].NetworkId;
        }

        return new NetworkItemInstanceDescriptor {
            Id = type.NetworkId,
            StackSize = checked((ushort)result.Count),
            AuxValue = unchecked((uint)result.Data),
            BlockRuntimeId = blockRuntimeId,
            UserDataBuffer = string.Empty
        };
    }

    private static UUID CreateProtocolUuid(Guid value) {
        Span<byte> bytes = stackalloc byte[16];
        value.TryWriteBytes(bytes);

        return new UUID {
            MostSignificantBits = BinaryPrimitives.ReadUInt64LittleEndian(bytes[..8]),
            LeastSignificantBits = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..])
        };
    }

    private static ItemType? ResolveItemType(string identifier) {
        ItemType? type = ItemType.Get(identifier);

        if (type is not null) {
            return type;
        }

        if (!identifier.Contains(':')) {
            type = ItemType.Get("minecraft:" + identifier);
        }

        return type;
    }

    private static string ResolveBlock(IReadOnlyList<string> tags) {
        for (int i = 0; i < tags.Count; i++) {
            switch (tags[i]) {
                case "crafting_table":
                    return "crafting_table";

                case "stonecutter":
                    return "stonecutter";

                case "furnace":
                    return "furnace";

                case "blast_furnace":
                    return "blast_furnace";

                case "smoker":
                    return "smoker";

                case "campfire":
                    return "campfire";

                case "cartography_table":
                    return "cartography_table";

                case "smithing_table":
                    return "smithing_table";
            }
        }

        return "crafting_table";
    }

    private static byte[] SerializePacket(Packet packet) {
        int size = 1024 * 256;

        while (true) {
            byte[] buffer = new byte[size];

            try {
                int offset = 0;
                BinaryWriter writer = new(buffer, ref offset);

                packet.Serialize(ref writer);

                return writer.GetProcessedBytes().ToArray();
            }
            catch (Exception ex) when (
                ex is ArgumentOutOfRangeException
                or IndexOutOfRangeException
            ) {
                size *= 2;
            }
        }
    }
}
