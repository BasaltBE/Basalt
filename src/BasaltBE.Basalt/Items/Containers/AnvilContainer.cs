using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Items.Containers;

internal sealed class AnvilContainer(Player player, BlockPos position) : BlockContainer(
    position,
    3,
    ContainerId.First,
    ContainerEnumName.AnvilInput,
    ContainerType.Anvil,
    slotOffset: 1,
    slotNames: [
        ContainerEnumName.AnvilInput,
        ContainerEnumName.AnvilMaterial,
        ContainerEnumName.AnvilResultPreview
    ]) {
    internal bool Crafting;

    internal void Use() {
        if (Random.Shared.NextSingle() >= 0.12f) return;

        BlockPermutation current = player.Dimension.GetPermutation(Position.X, Position.Y, Position.Z);
        BlockPermutation? damaged = AdvanceDamage(current);
        BlockPermutation updated = damaged ?? BlockStorage.Air;
        player.Dimension.SetPermutation(Position.X, Position.Y, Position.Z, updated);
        player.Dimension.Broadcast(new UpdateBlockPacket {
            Position = Position,
            BlockRuntimeId = unchecked((uint)updated.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, Position.X >> 4, Position.Z >> 4, additionalPacket: new LevelSoundEventPacket {
            SoundEvent = "random.anvil_use",
            Position = new Vec3 { X = Position.X + 0.5f, Y = Position.Y + 0.5f, Z = Position.Z + 0.5f },
            ActorUniqueId = -1
        });
        if (damaged is null) player.Dimension.UpdateNeighbors(Position.X, Position.Y, Position.Z);
    }

    internal static BlockPermutation? AdvanceDamage(BlockPermutation block) {
        string identifier = block.Identifier switch {
            "minecraft:anvil" => "minecraft:chipped_anvil",
            "minecraft:chipped_anvil" => "minecraft:damaged_anvil",
            "minecraft:damaged_anvil" => string.Empty,
            _ => block.Identifier
        };
        if (identifier.Length == 0) return null;
        return BlockPalette.Find(identifier, block.State) ?? BlockPalette.FindDefaultPermutation(identifier);
    }

    public override bool CanContain(int slot, ItemStack item) {
        if (slot == 2) return true;
        if (item.StackSize != 1) return false;
        if (slot == 0)
            return item.Type.MaxDurability > 0 || item.Type.Identifier is "minecraft:book" or "minecraft:enchanted_book" ||
                item.Type.PropertiesPayload.Components.Contains("minecraft:enchantable", StringComparer.Ordinal);
        return slot == 1 && (item.Type.Identifier == "minecraft:enchanted_book" || item.Type.MaxDurability > 0);
    }

    public void Refresh() {
        if (Crafting || !ReferenceEquals(player.OpenedContainer, this)) return;

        ItemStack? result = CreateResult(
            GetItem(0),
            GetItem(1),
            player.Network.Server.Enchantments,
            player.GameType == GameType.Creative,
            out _
        );
        ItemStack? current = GetItem(2);
        if (result is not null && current is not null && result.CanStackWith(current)) return;
        SetItem(2, result);
    }

    internal static ItemStack? CreateResult(
        ItemStack? target,
        ItemStack? material,
        EnchantmentRegistry enchantments,
        bool creative,
        out int levelCost
    ) {
        levelCost = 0;
        if (target is null || material is null || target.StackSize != 1 || material.StackSize != 1) return null;

        bool targetBook = target.Type.Identifier is "minecraft:book" or "minecraft:enchanted_book";
        bool materialBook = material.Type.Identifier == "minecraft:enchanted_book";
        bool sameDurableItem = !targetBook && target.Type == material.Type && target.Type.MaxDurability > 0;
        if (targetBook ? !materialBook : !materialBook && !sameDurableItem) return null;

        ItemStack result = targetBook && target.Type.Identifier == "minecraft:book"
            ? new ItemStack("minecraft:enchanted_book", 1, target.Metadata, target.Clone().Nbt)
            : target.Clone();
        bool changed = false;
        int operationCost = 0;

        if (sameDurableItem) {
            int maximum = result.Type.MaxDurability;
            int repaired = Math.Min(maximum, target.RemainingDurability + material.RemainingDurability + maximum * 12 / 100);
            int damage = Math.Max(0, maximum - repaired);
            if (damage < result.Damage) {
                result.SetDamage(damage);
                operationCost += 2;
                changed = true;
            }
        }

        foreach ((Enchantment enchantment, int sourceLevel) in ReadEnchantments(material, enchantments)) {
            if (!targetBook && !enchantment.AppliesTo(result.Type)) continue;

            int targetLevel = result.GetEnchantmentLevel(enchantment);
            int mergedLevel = targetLevel == sourceLevel
                ? Math.Min(targetLevel + 1, enchantment.MaximumLevel)
                : Math.Min(Math.Max(targetLevel, sourceLevel), enchantment.MaximumLevel);
            if (mergedLevel <= targetLevel || Conflicts(result, enchantment, enchantments)) continue;

            result.AddEnchantment(enchantment, mergedLevel);
            int enchantmentCost = materialBook
                ? Math.Max(1, enchantment.AnvilCost / 2)
                : enchantment.AnvilCost;
            operationCost += enchantmentCost * (mergedLevel - targetLevel);
            changed = true;
        }

        if (!changed) return null;

        int targetRepairCost = RepairCost(target);
        int materialRepairCost = RepairCost(material);
        levelCost = checked(targetRepairCost + materialRepairCost + operationCost);
        if (levelCost >= 40 && !creative) return null;

        result.Nbt ??= new CompoundTag();
        result.Nbt.Remove("RepairCost");
        result.Nbt.Add("RepairCost", new IntTag { Value = checked(Math.Max(targetRepairCost, materialRepairCost) * 2 + 1) });
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

    private static List<(Enchantment Enchantment, int Level)> ReadEnchantments(
        ItemStack item,
        EnchantmentRegistry enchantments
    ) {
        List<(Enchantment, int)> result = [];
        if (item.Nbt?.Get<ListTag>("ench") is { } native) {
            foreach (Tag entry in native.Value) {
                if (entry is not CompoundTag compound) continue;
                short id = compound.Get<ShortTag>("id")?.Value ?? -1;
                int level = compound.Get<ShortTag>("lvl")?.Value ?? 0;
                if (EnchantmentPalette.Find(id) is { } enchantment && level > 0) result.Add((enchantment, level));
            }
        }

        if (item.Nbt?.Get<ListTag>("basalt:enchantments") is { } custom) {
            foreach (Tag entry in custom.Value) {
                if (entry is not CompoundTag compound) continue;

                string identifier = compound.Get<StringTag>("id")?.Value ?? string.Empty;
                int level = compound.Get<IntTag>("lvl")?.Value ?? 0;
                if (enchantments.Find(identifier) is { } enchantment && level > 0) result.Add((enchantment, level));
            }
        }

        return result;
    }

    private static bool Conflicts(ItemStack item, Enchantment incoming, EnchantmentRegistry enchantments) {
        foreach (Enchantment existing in enchantments.Values) {
            if (item.GetEnchantmentLevel(existing) > 0 &&
                (incoming.IsIncompatibleWith(existing.Identifier) || existing.IsIncompatibleWith(incoming.Identifier)))
                return true;
        }

        return false;
    }

    private static int RepairCost(ItemStack item) => item.Nbt?.Get<IntTag>("RepairCost")?.Value ?? 0;
}
