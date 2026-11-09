using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items.Containers;

internal sealed class GrindstoneContainer(Player player, BlockPos position) : BlockContainer(
    position,
    3,
    ContainerId.First,
    ContainerEnumName.GrindstoneInput,
    ContainerType.Grindstone,
    slotOffset: 16,
    slotNames: [
        ContainerEnumName.GrindstoneInput,
        ContainerEnumName.GrindstoneAdditional,
        ContainerEnumName.GrindstoneResultPreview
    ]) {
    internal bool Crafting;

    public override bool CanContain(int slot, ItemStack item) => slot == 2 ||
        slot < 2 && item.StackSize == 1 && (item.Type.MaxDurability > 0 || item.Type.Identifier == "minecraft:enchanted_book");

    public void Refresh() {
        if (Crafting || !ReferenceEquals(player.OpenedContainer, this)) return;

        ItemStack? result = CreateResult(GetItem(0), GetItem(1), out _);
        ItemStack? current = GetItem(2);
        if (result is not null && current is not null && result.CanStackWith(current)) return;
        SetItem(2, result);
    }

    internal ItemStack? CreateResult(ItemStack? first, ItemStack? second, out int experience) {
        experience = 0;
        if (first is null) {
            (first, second) = (second, null);
        }
        if (first is null) return null;

        ItemStack result;
        if (second is not null) {
            if (first.Type.Identifier != second.Type.Identifier || first.Type.MaxDurability <= 0 ||
                first.Type.Identifier == "minecraft:enchanted_book")
                return null;

            result = first.Clone();
            int durability = Math.Min(
                result.Type.MaxDurability,
                first.RemainingDurability + second.RemainingDurability + result.Type.MaxDurability / 20
            );
            int damage = result.Type.MaxDurability - durability;
            if (damage > 0) result.SetDamage(damage);
        } else {
            if (!first.Enchanted && GetRepairCost(first) == 0) return null;

            if (first.Type.Identifier == "minecraft:enchanted_book")
                result = new ItemStack("minecraft:book", 1, first.Metadata, first.Clone().Nbt);
            else result = first.Clone();
        }

        experience = RemoveEnchantments(result);
        if (second is not null) experience += RemoveEnchantments(second.Clone());
        result.Nbt?.Remove("RepairCost");
        result.RefreshNetId();
        return result;
    }

    internal void ReturnItems() {
        if (player.GetTrait<PlayerInventoryTrait>() is not { } inventory) return;

        for (int slot = 0; slot < 2; slot++) {
            if (TakeItem(slot, ushort.MaxValue) is not { } item) continue;
            if (inventory.Inventory.AddItem(item) is { } remaining)
                new ItemEntity(remaining, player, Position.X, Position.Y, Position.Z).Spawn(player.Dimension);
        }
    }

    private static int RemoveEnchantments(ItemStack item) {
        int experience = 0;
        if (item.Nbt?.Get<ListTag>("ench") is { } enchantments) {
            for (int index = enchantments.Value.Count - 1; index >= 0; index--) {
                if (enchantments.Value[index] is not CompoundTag enchantment) continue;
                short id = enchantment.Get<ShortTag>("id")?.Value ?? -1;
                short level = enchantment.Get<ShortTag>("lvl")?.Value ?? 0;
                if (id is 27 or 28) continue;
                experience += EnchantmentExperience(level);
                enchantments.Value.RemoveAt(index);
            }
            if (enchantments.Value.Count == 0) item.Nbt.Remove("ench");
        }

        if (item.Nbt?.Get<ListTag>("basalt:enchantments") is { } customEnchantments) {
            for (int index = customEnchantments.Value.Count - 1; index >= 0; index--) {
                if (customEnchantments.Value[index] is not CompoundTag enchantment) continue;

                string identifier = enchantment.Get<StringTag>("id")?.Value ?? string.Empty;
                int level = enchantment.Get<IntTag>("lvl")?.Value ?? 0;
                if (identifier.EndsWith("curse_of_binding", StringComparison.Ordinal) ||
                    identifier.EndsWith("curse_of_vanishing", StringComparison.Ordinal))
                    continue;
                experience += EnchantmentExperience(level);
                customEnchantments.Value.RemoveAt(index);
            }
            if (customEnchantments.Value.Count == 0) item.Nbt.Remove("basalt:enchantments");
        }

        return experience;
    }

    private static int EnchantmentExperience(int level) => level > 0 ? Random.Shared.Next(1, Math.Min(level, byte.MaxValue) + 1) : 0;

    private static int GetRepairCost(ItemStack item) => item.Nbt?.Get<IntTag>("RepairCost")?.Value ?? 0;
}
