namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Containers;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Enchantment;
using Basalt.Core.Item.Traits;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class AnvilTrait : BlockTrait {
    public override bool Interactable => true;
    public static new readonly string Identifier = "anvil";
    public static new readonly string[] Types = [
        "minecraft:anvil",
        "minecraft:chipped_anvil",
        "minecraft:damaged_anvil"
    ];

    private const int InputSlot = 0;
    private const int MaterialSlot = 1;
    private const uint NetworkInputSlot = 0;
    private const uint NetworkMaterialSlot = 1;
    private BlockContainer? _container;
    private ItemStack? _result;
    private int _experienceCost;
    private int _materialConsumed;
    private string _rename = string.Empty;
    private bool _renameSet;
    private bool _updating;

    public AnvilTrait(Block block) : base(block) {
    }

    public BlockContainer? Container => _container;
    public ItemStack? Result => _result;
    public int ExperienceCost => _result is null ? 0 : _experienceCost;

    public override void OnInteract(BlockInteractDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) return;

        EnsureContainer(dimension, details.BlockPosition);
        _container?.Show(details.Player);
        SyncSlots();
    }

    public override void OnBreak(BlockBreakDetails details) {
        if (_container is null) return;

        foreach ((Player.Player player, _) in _container.GetAllOccupants().ToList()) {
            _container.Close(player);
        }

        for (int slot = InputSlot; slot <= MaterialSlot; slot++) {
            if (_container.GetItem(slot) is ItemStack item && item.StackSize > 0) {
                details.Player.DropItem(item);
            }
        }

        _container = null;
    }

    public void SetRename(string name) {
        _rename = name.Trim();
        _renameSet = true;
        UpdateResult();
    }

    public void CompleteResult() {
        int materialConsumed = _materialConsumed;
        _rename = string.Empty;
        _renameSet = false;
        _result = null;
        _updating = true;
        try {
            _container?.ClearSlot(InputSlot);
            if (_container?.GetItem(MaterialSlot) is not null && materialConsumed > 0) {
                _container.TakeItem(MaterialSlot, materialConsumed);
            }
        }
        finally {
            _updating = false;
        }
        _materialConsumed = 0;
    }

    public void RefreshResult() {
        UpdateResult();
        SyncSlots();
    }

    private void EnsureContainer(Dimension dimension, BlockPos position) {
        if (_container is not null) return;

        _container = new BlockContainer(dimension, position, ContainerType.ANVIL, 2) {
            OnContainerUpdated = OnContainerUpdated,
            OnViewerRemovedEvent = OnViewerRemoved
        };
    }

    private void OnViewerRemoved(BlockContainer container, Player.Player player) {
        if (container.GetAllOccupants().Count > 0) return;

        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        for (int slot = InputSlot; slot <= MaterialSlot; slot++) {
            if (container.GetItem(slot) is not ItemStack item) continue;

            if (inventory is null || !inventory.Container.AddItem(item)) {
                player.DropItem(item);
            }

            container.ClearSlot(slot);
        }

        _result = null;
    }

    private void OnContainerUpdated(BlockContainer container) {
        if (_updating) return;
        UpdateResult();
        SyncSlots();
    }

    private void UpdateResult() {
        if (_container is null || _updating) return;

        _updating = true;
        try {
            ItemStack? input = _container.GetItem(InputSlot);
            ItemStack? material = _container.GetItem(MaterialSlot);
            ItemStack? result = CreateResult(input, material);
            if (result is null) {
                _result = null;
            }
            else {
                _result = result;
            }
        }
        finally {
            _updating = false;
        }
    }

    private void SyncSlots() {
        if (_container is null) return;

        foreach ((Player.Player player, ContainerId containerId) in _container.GetAllOccupants()) {
            if (!player.Spawned) continue;

            player.Send(
                CreateSlotPacket(containerId, NetworkInputSlot, ContainerEnumName.AnvilInputContainer, null),
                CreateSlotPacket(containerId, NetworkInputSlot, ContainerEnumName.AnvilInputContainer,
                    _container.GetItem(InputSlot)),
                CreateSlotPacket(containerId, NetworkMaterialSlot, ContainerEnumName.AnvilMaterialContainer, null),
                CreateSlotPacket(containerId, NetworkMaterialSlot, ContainerEnumName.AnvilMaterialContainer,
                    _container.GetItem(MaterialSlot)),
                new ContainerSetDataPacket {
                    ContainerId = containerId,
                    Property = 0,
                    Value = _experienceCost
                });
        }
    }

    private static InventorySlotPacket CreateSlotPacket(
        ContainerId containerId,
        uint slot,
        ContainerEnumName containerName,
        ItemStack? item) {
        return new InventorySlotPacket {
            ContainerId = containerId,
            Slot = slot,
            Container = new FullContainerName {
                ContainerName = containerName,
                DynamicId = (uint)(byte)containerId
            },
            Item = item?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
            StorageItem = null
        };
    }

    private ItemStack? CreateResult(ItemStack? input, ItemStack? material) {
        _experienceCost = 0;
        _materialConsumed = 0;
        if (input is null || input.StackSize == 0) return null;

        ItemStackDurabilityTrait? inputDurability = input.GetTrait<ItemStackDurabilityTrait>();
        if (inputDurability is null &&
            input.Type.Components.GetComponent<ItemTypeDurabilityComponent>() is not null) {
            inputDurability = input.AddTrait(new ItemStackDurabilityTrait(input));
        }

        ItemStack result = input.Clone(1);
        ItemStackDurabilityTrait? resultDurability = result.GetTrait<ItemStackDurabilityTrait>();
        if (resultDurability is null &&
            result.Type.Components.GetComponent<ItemTypeDurabilityComponent>() is not null) {
            resultDurability = result.AddTrait(new ItemStackDurabilityTrait(result));
        }
        bool changed = false;
        int extraCost = 0;
        int materialConsumed = 0;

        if (material is not null && material.StackSize > 0) {
            if (material.Identifier == input.Identifier) {
                ItemStackDurabilityTrait? materialDurability = material.GetTrait<ItemStackDurabilityTrait>();

                if (inputDurability is not null && materialDurability is not null && resultDurability is not null) {
                    int bonus = Math.Max(1, (int)Math.Ceiling(inputDurability.GetMaxDurability() * 0.12));
                    int repair = materialDurability.GetMaxDurability() - materialDurability.GetCurrentDamage() + bonus;
                    int before = resultDurability.GetCurrentDamage();
                    resultDurability.Repair(repair);
                    changed = resultDurability.GetCurrentDamage() < before;
                }

                bool enchantmentsChanged = MergeEnchantments(input, material, result);
                changed |= enchantmentsChanged;
                if (enchantmentsChanged) {
                    extraCost += GetEnchantmentCost(input, material, false);
                }
                else if (changed) {
                    extraCost++;
                }
            }
            else if (IsRepairMaterial(input, material)) {
                if (inputDurability is not null && resultDurability is not null) {
                    int repair = Math.Max(1, inputDurability.GetMaxDurability() / 4);
                    while (materialConsumed < material.StackSize && resultDurability.GetCurrentDamage() > 0) {
                        int before = resultDurability.GetCurrentDamage();
                        resultDurability.Repair(repair);
                        if (resultDurability.GetCurrentDamage() >= before) break;

                        materialConsumed++;
                        extraCost++;
                    }

                    changed = materialConsumed > 0;
                }
            }
            else if (material.Type == ItemType.Get("minecraft:enchanted_book")) {
                bool enchantmentsChanged = MergeEnchantments(input, material, result);
                changed |= enchantmentsChanged;
                if (enchantmentsChanged) {
                    extraCost += GetEnchantmentCost(input, material, true);
                    materialConsumed = 1;
                }
            }
        }

        string? existingName = GetName(input);
        string? resultName = _renameSet ? _rename : existingName;
        if (_renameSet && !string.Equals(existingName, resultName, StringComparison.Ordinal)) {
            SetName(result, resultName);
            changed = true;
            extraCost++;
        }

        if (!changed) return null;

        if (material is not null && materialConsumed == 0 &&
            (material.Identifier == input.Identifier || material.Type == ItemType.Get("minecraft:enchanted_book"))) {
            materialConsumed = 1;
        }

        _materialConsumed = materialConsumed;
        _experienceCost = extraCost;
        return result;
    }

    private static int GetEnchantmentCost(ItemStack input, ItemStack material, bool book) {
        ItemStackEnchantmentTrait? materialEnchantments = material.GetTrait<ItemStackEnchantmentTrait>();
        if (materialEnchantments is null) return 0;

        ItemStackEnchantmentTrait? inputEnchantments = input.GetTrait<ItemStackEnchantmentTrait>();
        int cost = 0;
        foreach (EnchantmentInstance enchantment in materialEnchantments.Enchantments) {
            if (!enchantment.Type.CanApplyTo(input)) continue;

            EnchantmentInstance? existing = inputEnchantments?.GetEnchantment(enchantment.Type.Id);
            int targetLevel = existing?.Level ?? 0;
            int resultLevel = existing.HasValue && existing.Value.Level == enchantment.Level
                ? Math.Min(enchantment.Level + 1, enchantment.Type.MaxLevel)
                : Math.Max(targetLevel, enchantment.Level);
            int rarity = enchantment.Type.Identifier switch {
                "protection" or "sharpness" or "efficiency" or "power" or "piercing" => 1,
                "fire_protection" or "projectile_protection" or "feather_falling" or "smite" or
                    "bane_of_arthropods" or "knockback" or "unbreaking" or "quick_charge" or
                    "loyalty" or "lunge" => 2,
                "blast_protection" or "fire_aspect" or "fortune" or "looting" or "mending" or
                    "respiration" or "aqua_affinity" or "depth_strider" or "luck_of_the_sea" or
                    "lure" or "impaling" or "riptide" or "breach" or "density" or "wind_burst" or
                    "flame" or "multishot" => 4,
                _ => 8
            };

            if (book) rarity = Math.Max(1, rarity / 2);
            cost += rarity * Math.Max(0, resultLevel - targetLevel);
        }

        return cost;
    }

    private static bool MergeEnchantments(ItemStack input, ItemStack material, ItemStack result) {
        ItemStackEnchantmentTrait? sourceEnchantments = material.GetTrait<ItemStackEnchantmentTrait>();
        if (sourceEnchantments is null || sourceEnchantments.Enchantments.Count == 0) return false;

        ItemStackEnchantmentTrait resultEnchantments = result.GetTrait<ItemStackEnchantmentTrait>() ??
            result.AddTrait(new ItemStackEnchantmentTrait(result));
        bool changed = false;

        foreach (EnchantmentInstance enchantment in sourceEnchantments.Enchantments) {
            if (!enchantment.Type.CanApplyTo(input)) continue;

            EnchantmentInstance? existing = resultEnchantments.GetEnchantment(enchantment.Type.Id);
            if (existing.HasValue && enchantment.Type.ConflictsWith(existing.Value.Type)) continue;

            int level = existing.HasValue && existing.Value.Level == enchantment.Level
                ? Math.Min(enchantment.Level + 1, enchantment.Type.MaxLevel)
                : Math.Max(existing?.Level ?? 0, enchantment.Level);
            if (!existing.HasValue || existing.Value.Level != level) {
                resultEnchantments.SetEnchantment(enchantment.Type.Id, level);
                changed = true;
            }
        }

        return changed;
    }

    private static bool IsRepairMaterial(ItemStack input, ItemStack material) {
        if (!ItemIdentifierExtensions.TryFromIdentifier(input.Identifier, out ItemIdentifier inputIdentifier) ||
            !ItemIdentifierExtensions.TryFromIdentifier(material.Identifier,
                out ItemIdentifier materialIdentifier)) {
            return false;
        }

        return inputIdentifier switch {
            ItemIdentifier.WoodenSword or ItemIdentifier.WoodenPickaxe or ItemIdentifier.WoodenAxe or
            ItemIdentifier.WoodenShovel or ItemIdentifier.WoodenHoe => materialIdentifier is ItemIdentifier.OakPlanks or
                ItemIdentifier.SprucePlanks or ItemIdentifier.BirchPlanks or ItemIdentifier.JunglePlanks or
                ItemIdentifier.AcaciaPlanks or ItemIdentifier.DarkOakPlanks or ItemIdentifier.MangrovePlanks or
                ItemIdentifier.CherryPlanks or ItemIdentifier.BambooPlanks or ItemIdentifier.CrimsonPlanks or
                ItemIdentifier.WarpedPlanks,
            ItemIdentifier.StoneSword or ItemIdentifier.StonePickaxe or ItemIdentifier.StoneAxe or
            ItemIdentifier.StoneShovel or ItemIdentifier.StoneHoe => materialIdentifier == ItemIdentifier.Cobblestone,
            ItemIdentifier.IronSword or ItemIdentifier.IronPickaxe or ItemIdentifier.IronAxe or
            ItemIdentifier.IronShovel or ItemIdentifier.IronHoe or ItemIdentifier.IronHelmet or
            ItemIdentifier.IronChestplate or ItemIdentifier.IronLeggings or ItemIdentifier.IronBoots =>
                materialIdentifier == ItemIdentifier.IronIngot,
            ItemIdentifier.GoldenSword or ItemIdentifier.GoldenPickaxe or ItemIdentifier.GoldenAxe or
            ItemIdentifier.GoldenShovel or ItemIdentifier.GoldenHoe or ItemIdentifier.GoldenHelmet or
            ItemIdentifier.GoldenChestplate or ItemIdentifier.GoldenLeggings or ItemIdentifier.GoldenBoots =>
                materialIdentifier == ItemIdentifier.GoldIngot,
            ItemIdentifier.DiamondSword or ItemIdentifier.DiamondPickaxe or ItemIdentifier.DiamondAxe or
            ItemIdentifier.DiamondShovel or ItemIdentifier.DiamondHoe or ItemIdentifier.DiamondHelmet or
            ItemIdentifier.DiamondChestplate or ItemIdentifier.DiamondLeggings or ItemIdentifier.DiamondBoots =>
                materialIdentifier == ItemIdentifier.Diamond,
            ItemIdentifier.NetheriteSword or ItemIdentifier.NetheritePickaxe or ItemIdentifier.NetheriteAxe or
            ItemIdentifier.NetheriteShovel or ItemIdentifier.NetheriteHoe or ItemIdentifier.NetheriteHelmet or
            ItemIdentifier.NetheriteChestplate or ItemIdentifier.NetheriteLeggings or ItemIdentifier.NetheriteBoots =>
                materialIdentifier == ItemIdentifier.NetheriteIngot,
            _ => false
        };
    }

    private static string? GetName(ItemStack item) {
        return item.Storage?.Get<CompoundTag>("display")?.Get<StringTag>("Name")?.Value;
    }

    private static void SetName(ItemStack item, string? name) {
        CompoundTag storage = item.Storage ?? new CompoundTag();
        CompoundTag display = storage.Get<CompoundTag>("display") ?? new CompoundTag();

        if (string.IsNullOrEmpty(name)) {
            display.Values.Remove("Name");
        }
        else {
            display.Set("Name", new StringTag { Value = name });
        }

        if (display.Values.Count == 0) {
            storage.Values.Remove("display");
        }
        else {
            storage.Set("display", display);
        }

        item.Storage = storage.Values.Count == 0 ? null : storage;
    }
}
