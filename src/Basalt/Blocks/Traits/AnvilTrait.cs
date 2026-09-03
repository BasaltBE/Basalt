namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Containers;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
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
    private const int ResultSlot = 2;

    private BlockContainer? _container;
    private string _rename = string.Empty;
    private bool _renameSet;
    private bool _updating;

    public AnvilTrait(Block block) : base(block) {
    }

    public BlockContainer? Container => _container;

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
        _rename = string.Empty;
        _renameSet = false;
        _container?.ClearSlot(InputSlot);
        _container?.ClearSlot(MaterialSlot);
    }

    public void RefreshResult() {
        UpdateResult();
        SyncSlots();
    }

    private void EnsureContainer(Dimension dimension, BlockPos position) {
        if (_container is not null) return;

        _container = new BlockContainer(dimension, position, ContainerType.ANVIL, 3) {
            OnContainerUpdated = OnContainerUpdated
        };
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
            ItemStack? result = CreateResult(_container.GetItem(InputSlot), _container.GetItem(MaterialSlot));
            if (result is null) {
                _container.ClearSlot(ResultSlot);
            }
            else {
                _container.SetItem(ResultSlot, result);
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
                CreateSlotPacket(containerId, ContainerEnumName.AnvilInputContainer, _container.GetItem(InputSlot)),
                CreateSlotPacket(containerId, ContainerEnumName.AnvilMaterialContainer, _container.GetItem(MaterialSlot)),
                CreateSlotPacket(containerId, ContainerEnumName.AnvilResultPreviewContainer, _container.GetItem(ResultSlot)));
        }
    }

    private static InventorySlotPacket CreateSlotPacket(
        ContainerId containerId,
        ContainerEnumName containerName,
        ItemStack? item) {
        return new InventorySlotPacket {
            ContainerId = containerId,
            Slot = 0,
            Container = new FullContainerName {
                ContainerName = containerName,
                DynamicId = (uint)(byte)containerId
            },
            Item = item?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
            StorageItem = null
        };
    }

    private ItemStack? CreateResult(ItemStack? input, ItemStack? material) {
        if (input is null || input.StackSize == 0) return null;

        ItemStack result = input.Clone(1);
        bool changed = false;

        if (material is not null && material.StackSize > 0) {
            if (material.Identifier == input.Identifier) {
                ItemStackDurabilityTrait? inputDurability = input.GetTrait<ItemStackDurabilityTrait>();
                ItemStackDurabilityTrait? materialDurability = material.GetTrait<ItemStackDurabilityTrait>();
                ItemStackDurabilityTrait? resultDurability = result.GetTrait<ItemStackDurabilityTrait>();

                if (inputDurability is not null && materialDurability is not null && resultDurability is not null) {
                    int bonus = Math.Max(1, (int)Math.Ceiling(inputDurability.GetMaxDurability() * 0.12));
                    int repair = materialDurability.GetMaxDurability() - materialDurability.GetCurrentDamage() + bonus;
                    int before = resultDurability.GetCurrentDamage();
                    resultDurability.Repair(repair);
                    changed = resultDurability.GetCurrentDamage() < before;
                }
            }
            else if (material.Identifier == "minecraft:enchanted_book") {
                ItemStackEnchantmentTrait? sourceEnchantments = material.GetTrait<ItemStackEnchantmentTrait>();
                ItemStackEnchantmentTrait? resultEnchantments = result.GetTrait<ItemStackEnchantmentTrait>();

                if (sourceEnchantments is not null && resultEnchantments is not null) {
                    foreach (EnchantmentInstance enchantment in sourceEnchantments.Enchantments) {
                        EnchantmentInstance? existing = resultEnchantments.GetEnchantment(enchantment.Type.Id);
                        int level = existing.HasValue && existing.Value.Level == enchantment.Level
                            ? Math.Min(enchantment.Level + 1, enchantment.Type.MaxLevel)
                            : Math.Max(existing?.Level ?? 0, enchantment.Level);
                        resultEnchantments.SetEnchantment(enchantment.Type.Id, level);
                        changed = true;
                    }
                }
            }
        }

        string? existingName = GetName(input);
        string? resultName = _renameSet ? _rename : existingName;
        if (_renameSet && !string.Equals(existingName, resultName, StringComparison.Ordinal)) {
            SetName(result, resultName);
            changed = true;
        }

        return changed ? result : null;
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
