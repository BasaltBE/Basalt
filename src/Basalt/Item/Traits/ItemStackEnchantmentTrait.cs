namespace Basalt.Core.Item.Traits;

using Basalt.Core.Item.Enchantment;
using Basalt.BedrockProtocol.NBT;

/// <summary>
/// Holds enchantments on an item stack.
/// Attached on items with the "minecraft:enchantable" component.
/// </summary>
public sealed class ItemStackEnchantmentTrait(ItemStack itemStack) : ItemTrait(itemStack) {
    public new static string Identifier => "enchantments";
    public new static readonly string[] Tags = ["minecraft:bookshelf_books"];

    private readonly List<EnchantmentInstance> _enchantments = [];

    public IReadOnlyList<EnchantmentInstance> Enchantments => _enchantments;

    public bool HasEnchantment(int id) {
        for (int i = 0; i < _enchantments.Count; i++) {
            if (_enchantments[i].Type.Id == id) return true;
        }
        return false;
    }

    public EnchantmentInstance? GetEnchantment(int id) {
        for (int i = 0; i < _enchantments.Count; i++) {
            if (_enchantments[i].Type.Id == id) return _enchantments[i];
        }
        return null;
    }

    public int GetLevel(int id) {
        for (int i = 0; i < _enchantments.Count; i++) {
            if (_enchantments[i].Type.Id == id) return _enchantments[i].Level;
        }
        return 0;
    }

    public int GetLevel(string identifier) {
        for (int i = 0; i < _enchantments.Count; i++) {
            if (string.Equals(_enchantments[i].Type.Identifier, identifier, StringComparison.Ordinal)) {
                return _enchantments[i].Level;
            }
        }

        return 0;
    }

    public void AddEnchantment(EnchantmentInstance enchantment) {
        for (int i = 0; i < _enchantments.Count; i++) {
            if (_enchantments[i].Type.Id == enchantment.Type.Id) {
                _enchantments[i] = enchantment;
                SetTag();
                return;
            }
        }

        _enchantments.Add(enchantment);
        SetTag();
    }

    public void SetEnchantment(int id, int level) {
        RemoveEnchantment(id);

        EnchantmentInstance? instance = EnchantmentInstance.Create(id, level);
        if (instance.HasValue) {
            _enchantments.Add(instance.Value);
            SetTag();
        }
    }

    public bool RemoveEnchantment(int id) {
        for (int i = 0; i < _enchantments.Count; i++) {
            if (_enchantments[i].Type.Id == id) {
                _enchantments.RemoveAt(i);
                SetTag();
                return true;
            }
        }
        return false;
    }

    public void Clear() {
        _enchantments.Clear();
        SetTag();
    }

    public float GetAttackBonus() {
        float total = 0f;
        for (int i = 0; i < _enchantments.Count; i++)
            total += _enchantments[i].GetAttackBonus();
        return total;
    }

    public float GetProtectionBonus() {
        float total = 0f;
        for (int i = 0; i < _enchantments.Count; i++)
            total += _enchantments[i].GetProtectionBonus();
        return total;
    }

    public float GetMiningSpeedBonus() {
        float total = 0f;
        for (int i = 0; i < _enchantments.Count; i++)
            total += _enchantments[i].GetMiningSpeedBonus();
        return total;
    }

    public void OnBlockBreak(BlockBreakEnchantmentContext ctx) {
        for (int i = 0; i < _enchantments.Count; i++)
            _enchantments[i].Type.OnBlockBreak(_enchantments[i].Level, ctx);
    }

    public void OnAttackEntity(AttackEntityEnchantmentContext ctx) {
        for (int i = 0; i < _enchantments.Count; i++)
            _enchantments[i].Type.OnAttackEntity(_enchantments[i].Level, ctx);
    }

    public void OnHurt(HurtEnchantmentContext ctx) {
        for (int i = 0; i < _enchantments.Count; i++)
            _enchantments[i].Type.OnHurt(_enchantments[i].Level, ctx);
    }

    public void OnTick(TickEnchantmentContext ctx) {
        for (int i = 0; i < _enchantments.Count; i++)
            _enchantments[i].Type.OnTick(_enchantments[i].Level, ctx);
    }

    public override void OnRead(CompoundTag tag) {
        _enchantments.Clear();

        ListTag? enchList = tag.Get<ListTag>("ench");
        if (enchList is null) return;

        for (int i = 0; i < enchList.Values.Count; i++) {
            if (enchList.Values[i] is not CompoundTag entry) continue;

            int id = entry.Get<ShortTag>("id")?.Value ?? -1;
            int lvl = entry.Get<ShortTag>("lvl")?.Value ?? 1;

            EnchantmentInstance? instance = EnchantmentInstance.Create(id, lvl);
            if (instance.HasValue) {
                _enchantments.Add(instance.Value);
            }
        }
    }

    public override void OnWrite(CompoundTag tag) {
        if (_enchantments.Count == 0) return;
        tag.Set("ench", BuildEnchListTag());
    }

    /// <summary>
    /// Builds a CompoundTag with the "ench" list for creative content.
    /// </summary>
    public static CompoundTag BuildEnchantmentNbt(IReadOnlyList<EnchantmentInstance> enchantments) {
        CompoundTag nbt = new();
        ListTag enchList = new();

        for (int i = 0; i < enchantments.Count; i++) {
            CompoundTag entry = new();
            entry.Set("id", new ShortTag { Value = (short)enchantments[i].Type.Id });
            entry.Set("lvl", new ShortTag { Value = (short)enchantments[i].Level });
            enchList.Values.Add(entry);
        }

        nbt.Set("ench", enchList);
        return nbt;
    }

    private ListTag BuildEnchListTag() {
        ListTag enchList = new();
        for (int i = 0; i < _enchantments.Count; i++) {
            CompoundTag entry = new();
            entry.Set("id", new ShortTag { Value = (short)_enchantments[i].Type.Id });
            entry.Set("lvl", new ShortTag { Value = (short)_enchantments[i].Level });
            enchList.Values.Add(entry);
        }
        return enchList;
    }

    private void SetTag() {
        CompoundTag nbt = ItemStack.Storage ?? new CompoundTag();

        if (_enchantments.Count == 0) {
            nbt.Values.Remove("ench");
        }
        else {
            nbt.Set("ench", BuildEnchListTag());
        }

        if (nbt.Values.Count > 0) {
            ItemStack.Storage = nbt;
        }
    }
}
