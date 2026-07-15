namespace Basalt.Core.Item.Traits;

using Basalt.Core.Item.Enchantment;
using Basalt.Protocol.Nbt;

/// <summary>
/// Trait that holds enchantments on an item stack.
/// Attached to items with the "minecraft:enchantable" component.
/// </summary>
public sealed class ItemStackEnchantmentTrait : ItemTrait
{
  public new static string Identifier => "enchantments";
  public new static readonly string[] Tags = ["minecraft:bookshelf_books"];

  private readonly List<EnchantmentInstance> _enchantments = [];

  public IReadOnlyList<EnchantmentInstance> Enchantments => _enchantments;

  public ItemStackEnchantmentTrait(ItemStack itemStack) : base(itemStack)
  {
  }

  public void AddEnchantment(EnchantmentInstance enchantment)
  {
    for (int i = 0; i < _enchantments.Count; i++)
    {
      if (_enchantments[i].Type.Id == enchantment.Type.Id)
      {
        _enchantments[i] = enchantment;
        SetTag();
        return;
      }
    }

    _enchantments.Add(enchantment);
    SetTag();
  }

  public bool RemoveEnchantment(int enchantmentId)
  {
    for (int i = 0; i < _enchantments.Count; i++)
    {
      if (_enchantments[i].Type.Id == enchantmentId)
      {
        _enchantments.RemoveAt(i);
        SetTag();
        return true;
      }
    }

    return false;
  }

  public bool HasEnchantment(int enchantmentId)
  {
    for (int i = 0; i < _enchantments.Count; i++)
    {
      if (_enchantments[i].Type.Id == enchantmentId) return true;
    }

    return false;
  }

  public EnchantmentInstance? GetEnchantment(int enchantmentId)
  {
    for (int i = 0; i < _enchantments.Count; i++)
    {
      if (_enchantments[i].Type.Id == enchantmentId) return _enchantments[i];
    }

    return null;
  }

  public int GetLevel(int enchantmentId)
  {
    for (int i = 0; i < _enchantments.Count; i++)
    {
      if (_enchantments[i].Type.Id == enchantmentId) return _enchantments[i].Level;
    }

    return 0;
  }

  public void ClearEnchantments()
  {
    _enchantments.Clear();
    SetTag();
  }

  public override void OnRead(CompoundTag tag)
  {
    _enchantments.Clear();
    ListTag? enchList = tag.Get<ListTag>("ench");
    if (enchList is null) return;

    for (int i = 0; i < enchList.Values.Count; i++)
    {
      if (enchList.Values[i] is not CompoundTag entry) continue;

      int id = entry.Get<ShortTag>("id")?.Value ?? -1;
      int lvl = entry.Get<ShortTag>("lvl")?.Value ?? 1;

      EnchantmentInstance? instance = EnchantmentInstance.Create(id, lvl);
      if (instance.HasValue)
      {
        _enchantments.Add(instance.Value);
      }
    }
  }

  public override void OnWrite(CompoundTag tag)
  {
    if (_enchantments.Count == 0) return;

    ListTag enchList = new();
    for (int i = 0; i < _enchantments.Count; i++)
    {
      CompoundTag entry = new();
      entry.Set("id", new ShortTag { Value = (short)_enchantments[i].Type.Id });
      entry.Set("lvl", new ShortTag { Value = (short)_enchantments[i].Level });
      enchList.Values.Add(entry);
    }

    tag.Set("ench", enchList);
  }

  /// <summary>
  /// Builds the "ench" NBT tag for this enchantment set (used by creative content).
  /// </summary>
  public static CompoundTag BuildEnchantmentNbt(IReadOnlyList<EnchantmentInstance> enchantments)
  {
    CompoundTag nbt = new();
    ListTag enchList = new();

    for (int i = 0; i < enchantments.Count; i++)
    {
      CompoundTag entry = new();
      entry.Set("id", new ShortTag { Value = (short)enchantments[i].Type.Id });
      entry.Set("lvl", new ShortTag { Value = (short)enchantments[i].Level });
      enchList.Values.Add(entry);
    }

    nbt.Set("ench", enchList);
    return nbt;
  }

  private void SetTag()
  {
    CompoundTag nbt = ItemStack.ExtraData?.Nbt ?? new CompoundTag();

    if (_enchantments.Count == 0)
    {
      nbt.Values.Remove("ench");
    }
    else
    {
      ListTag enchList = new();
      for (int i = 0; i < _enchantments.Count; i++)
      {
        CompoundTag entry = new();
        entry.Set("id", new ShortTag { Value = (short)_enchantments[i].Type.Id });
        entry.Set("lvl", new ShortTag { Value = (short)_enchantments[i].Level });
        enchList.Values.Add(entry);
      }

      nbt.Set("ench", enchList);
    }

    if (nbt.Values.Count > 0)
    {
      ItemStack.SetExtraData(new Protocol.Types.ItemInstanceUserData
      {
        Nbt = nbt,
        CanPlaceOn = ItemStack.ExtraData?.CanPlaceOn ?? [],
        CanDestroy = ItemStack.ExtraData?.CanDestroy ?? [],
        Ticking = ItemStack.ExtraData?.Ticking
      });
    }
  }
}
