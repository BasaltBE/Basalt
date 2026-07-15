namespace Basalt.Core.Crafting;

using Basalt.Core.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

public sealed class CraftingRegistry
{
  private static CraftingRegistry? _instance;
  public static CraftingRegistry Instance => _instance ?? throw new InvalidOperationException("CraftingRegistry not initialized.");

  private readonly List<CraftingRecipe> _recipes = [];
  private readonly Dictionary<string, int> _identifierToIndex = new(StringComparer.Ordinal);
  private readonly Dictionary<uint, int> _networkIdToIndex = [];
  private byte[]? _cachedPayload;

  public static void Initialize()
  {
    _instance = new CraftingRegistry();
  }

  public void RegisterShaped(
    string identifier,
    IReadOnlyList<string> pattern,
    IReadOnlyDictionary<char, RecipeIngredient> key,
    RecipeResult result,
    IReadOnlyList<string>? tags = null,
    int priority = 0)
  {
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
    int priority = 0)
  {
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

  public void AddRecipe(CraftingRecipe recipe)
  {
    _recipes.Add(recipe);
    int index = _recipes.Count - 1;
    _identifierToIndex[recipe.Identifier] = index;
    InvalidateCache();
  }

  public CraftingRecipe? GetByNetworkId(uint networkId)
  {
    if (_networkIdToIndex.TryGetValue(networkId, out int index) && index < _recipes.Count)
    {
      return _recipes[index];
    }
    return null;
  }

  public CraftingRecipe? GetByIdentifier(string identifier)
  {
    if (_identifierToIndex.TryGetValue(identifier, out int index))
    {
      return _recipes[index];
    }
    return null;
  }

  public IReadOnlyList<CraftingRecipe> GetAll() => _recipes;

  public byte[] GetCraftingDataPayload()
  {
    if (_cachedPayload is not null) return _cachedPayload;

    List<CraftingDataEntry> entries = [];
    _networkIdToIndex.Clear();
    uint networkId = 1;

    for (int i = 0; i < _recipes.Count; i++)
    {
      CraftingRecipe recipe = _recipes[i];
      CraftingDataEntry? entry = BuildEntry(recipe, networkId);
      if (entry is null)
      {
        networkId++;
        continue;
      }

      entries.Add(entry);
      _networkIdToIndex[networkId] = i;
      networkId++;
    }

    CraftingDataPacket packet = new()
    {
      Recipes = entries,
      ClearRecipes = true
    };

    _cachedPayload = SerializePacket(packet);
    Logger.Info("CraftingData: {0} recipes loaded ({1} skipped), {2} bytes.", entries.Count, _recipes.Count - entries.Count, _cachedPayload.Length);
    return _cachedPayload;
  }

  private void InvalidateCache()
  {
    _cachedPayload = null;
    _networkIdToIndex.Clear();
  }

  private static CraftingDataEntry? BuildEntry(CraftingRecipe recipe, uint networkId)
  {
    ItemType? resultType = ItemType.Get(recipe.Result.Item);
    if (resultType is null)
    {
      Logger.Warn("Crafting: skipping '{0}', result item '{1}' not found.", recipe.Identifier, recipe.Result.Item);
      return null;
    }

    byte[] uuid = new byte[16];
    BitConverter.TryWriteBytes(uuid, networkId);

    return recipe.Type switch
    {
      RecipeType.Shaped => BuildShapedEntry(recipe, resultType, networkId, uuid),
      RecipeType.Shapeless => BuildShapelessEntry(recipe, resultType, networkId, uuid),
      _ => null
    };
  }

  private static CraftingDataEntry? BuildShapedEntry(CraftingRecipe recipe, ItemType resultType, uint networkId, byte[] uuid)
  {
    if (recipe.Pattern.Count == 0) return null;

    int height = recipe.Pattern.Count;
    int width = recipe.Pattern[0].Length;

    List<ItemDescriptorCount> input = new(width * height);
    for (int row = 0; row < height; row++)
    {
      string line = recipe.Pattern[row];
      for (int col = 0; col < width; col++)
      {
        char symbol = col < line.Length ? line[col] : ' ';
        if (symbol == ' ')
        {
          input.Add(new ItemDescriptorCount { DescriptorType = 0, Count = 0 });
          continue;
        }

        if (!recipe.Key.TryGetValue(symbol, out RecipeIngredient? ingredient))
        {
          input.Add(new ItemDescriptorCount { DescriptorType = 0, Count = 0 });
          continue;
        }

        ItemDescriptorCount? descriptor = BuildDescriptor(ingredient);
        if (descriptor is null)
        {
          Logger.Warn("Crafting: skipping '{0}', ingredient for '{1}' not found.", recipe.Identifier, symbol);
          return null;
        }
        input.Add(descriptor);
      }
    }

    ShapedRecipeData data = new()
    {
      RecipeId = recipe.Identifier,
      Width = width,
      Height = height,
      Input = input,
      Output = [BuildResultItem(resultType, recipe.Result)],
      Uuid = uuid,
      Block = ResolveBlock(recipe.Tags),
      Priority = recipe.Priority,
      AssumeSymmetry = false,
      UnlockRequirement = new RecipeUnlockingRequirement(),
      RecipeNetworkId = networkId
    };

    return new CraftingDataEntry
    {
      RecipeType = CraftingDataRecipeType.Shaped,
      Shaped = data
    };
  }

  private static CraftingDataEntry? BuildShapelessEntry(CraftingRecipe recipe, ItemType resultType, uint networkId, byte[] uuid)
  {
    List<ItemDescriptorCount> input = new(recipe.Ingredients.Count);
    for (int i = 0; i < recipe.Ingredients.Count; i++)
    {
      ItemDescriptorCount? descriptor = BuildDescriptor(recipe.Ingredients[i]);
      if (descriptor is null)
      {
        Logger.Warn("Crafting: skipping '{0}', ingredient '{1}' not found.", recipe.Identifier, recipe.Ingredients[i].Item ?? recipe.Ingredients[i].Tag);
        return null;
      }
      input.Add(descriptor);
    }

    ShapelessRecipeData data = new()
    {
      RecipeId = recipe.Identifier,
      Input = input,
      Output = [BuildResultItem(resultType, recipe.Result)],
      Uuid = uuid,
      Block = ResolveBlock(recipe.Tags),
      Priority = recipe.Priority,
      UnlockRequirement = new RecipeUnlockingRequirement(),
      RecipeNetworkId = networkId
    };

    return new CraftingDataEntry
    {
      RecipeType = CraftingDataRecipeType.Shapeless,
      Shapeless = data
    };
  }

  private static ItemDescriptorCount? BuildDescriptor(RecipeIngredient ingredient)
  {
    if (ingredient.Tag is not null)
    {
      return new ItemDescriptorCount
      {
        DescriptorType = 5,
        Text = ingredient.Tag,
        Count = ingredient.Count
      };
    }

    if (ingredient.Item is null) return null;

    ItemType? type = ItemType.Get(ingredient.Item);
    if (type is null) return null;

    return new ItemDescriptorCount
    {
      DescriptorType = 1,
      NetworkId = checked((short)type.NetworkId),
      MetadataValue = checked((short)ingredient.Data),
      Count = ingredient.Count
    };
  }

  private static LegacyNetworkItemStackDescriptor BuildResultItem(ItemType type, RecipeResult result)
  {
    int blockRuntimeId = 0;
    if (type.BlockType is not null && type.BlockType.Permutations.Count > 0)
    {
      blockRuntimeId = type.BlockType.Permutations[0].NetworkId;
    }

    return new LegacyNetworkItemStackDescriptor
    {
      NetworkId = type.NetworkId,
      StackSize = checked((ushort)result.Count),
      Metadata = result.Data,
      NetworkBlockId = blockRuntimeId,
      ExtraData = null
    };
  }

  private static string ResolveBlock(IReadOnlyList<string> tags)
  {
    for (int i = 0; i < tags.Count; i++)
    {
      switch (tags[i])
      {
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

  private static byte[] SerializePacket(DataPacket packet)
  {
    int size = 1024 * 256;
    while (true)
    {
      byte[] buffer = new byte[size];
      try
      {
        int offset = 0;
        BinaryWriter writer = new(buffer, ref offset);
        packet.Serialize(writer);
        return writer.GetProcessedBytes().ToArray();
      }
      catch (Exception ex) when (ex is ArgumentOutOfRangeException or IndexOutOfRangeException)
      {
        size *= 2;
      }
    }
  }
}
