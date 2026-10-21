using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class InventoryTransactionHandler {
    private const float BlockReachSquared = 6 * 6;
    [ThreadStatic]
    private static UpdateBlockPacket? blockUpdate;

    public void Handle(NethernetPeer peer, Player? player, InventoryTransactionPacket packet) {
        if (player is null)
            throw new InvalidDataException("Inventory transaction arrived before player login.");
        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (player.Disposed || !player.Spawned)
            return;

        player.GetTrait<PlayerInventoryTrait>()?.QueueTransaction(
            packet.Transaction,
            packet.LegacyRequestId,
            packet.LegacySetItemSlots
        );
    }

    internal static void ProcessTransaction(
        Player player,
        InventoryTransactionData transaction,
        int legacyRequestId,
        LegacySetSlot[]? legacySetItemSlots
    ) {
        lock (player.Sync) {
            PlayerInventoryTrait? inventory = player.GetTrait<PlayerInventoryTrait>();
            if (player.Disposed || !player.Spawned || inventory is null)
                return;

            if (transaction is ItemReleaseInventoryTransaction release) {
                if (release.ActionType == ItemReleaseInventoryTransactionActionType.Release)
                    player.GetTrait<PlayerHungerTrait>()?.CancelEating();
                return;
            }

            if (transaction is NormalTransactionData normal &&
                normal.Actions.Actions.Any(action => action.Source.Type == InventorySourceType.WorldInteraction)) {
                DropWorldItem(player, inventory, normal.Actions.Actions, legacyRequestId);
                return;
            }

            if (transaction is NormalTransactionData armorTransaction &&
                EquipArmor(player, inventory, armorTransaction.Actions.Actions))
                return;

            if (transaction is ItemUseInventoryTransaction legacyArmorUse &&
                EquipArmorFromLegacyUse(player, inventory, legacyArmorUse, legacyRequestId, legacySetItemSlots))
                return;

            if (legacyRequestId != 0 && transaction is not (ItemUseInventoryTransaction or ItemUseOnActorInventoryTransaction)) {
                player.Network.SendPacket(player, new ItemStackResponsePacket {
                    Responses = [new ItemStackResponseInfo {
                        ClientRequestId = legacyRequestId,
                        Result = ItemStackNetResult.Error
                    }]
                });
                inventory.Inventory.Update();
                return;
            }

            var (slot, hand, descriptor, origin) = transaction switch {
                ItemUseInventoryTransaction use => (use.Slot, use.Hand, use.Item, use.FromPosition),
                ItemUseOnActorInventoryTransaction actor => (actor.Slot, actor.Hand, actor.Item, actor.FromPosition),
                _ => (-1, HandSlot.Mainhand, null, null)
            };
            if (transaction is ItemUseInventoryTransaction { ActionType: ItemUseInventoryTransactionActionType.Use } armorUse &&
                armorUse.Hand == HandSlot.Mainhand &&
                (uint)armorUse.Slot < inventory.Inventory.ContainerSize &&
                armorUse.Slot != inventory.SelectedSlot &&
                float.IsFinite(armorUse.FromPosition.X) && float.IsFinite(armorUse.FromPosition.Y) &&
                float.IsFinite(armorUse.FromPosition.Z) &&
                inventory.Inventory.GetItem(armorUse.Slot)?.Type.ArmorSlot is not null &&
                inventory.MatchesItem(armorUse.Slot, armorUse.Item, out _) &&
                player.GetTrait<EntityArmorTrait>() is { } armor) {
                armor.Equip(inventory, armorUse.Slot);
                return;
            }

            bool attack = transaction is ItemUseOnActorInventoryTransaction {
                ActionType: ItemUseOnActorInventoryTransactionActionType.Attack
            };
            if (descriptor is null || origin is null || hand != HandSlot.Mainhand ||
                (uint)slot >= 9 || slot != inventory.SelectedSlot ||
                !float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || !float.IsFinite(origin.Z)) {
                if (attack)
                    Logger.Scoped("Network").Info($"Rejected attack from {player.Username}: slot={slot}, selected={inventory.SelectedSlot}, hand={hand}.");
                return;
            }
            ItemStack? item;
            if (attack) {
                item = inventory.Inventory.GetItem(slot);
            }
            else if (!inventory.MatchesItem(slot, descriptor, out item)) {
                if (transaction is ItemUseInventoryTransaction { ActionType: ItemUseInventoryTransactionActionType.Place } predicted)
                    RepairPlacement(player, predicted);
                inventory.Inventory.UpdateSlot(slot);
                return;
            }

            switch (transaction) {
                case ItemUseInventoryTransaction use:
                    if (use.ActionType == ItemUseInventoryTransactionActionType.Use) {
                        if (item is not null && item.Type.FoodNutrition > 0) {
                            player.GetTrait<PlayerHungerTrait>()?.StartEating(slot, item);
                            break;
                        }

                        int durabilityDamage = item?.Type.MaxDurability > 0 && item.Type.ArmorSlot is null ? 1 : 0;
                        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
                            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.Use, durabilityDamage);
                            player.Network.Server.Events.Emit(itemUse);
                            durabilityDamage = itemUse.DurabilityDamage;
                            if (itemUse.IsCanceled()) {
                                item?.DamageDurability(durabilityDamage);
                                break;
                            }
                        }
                        player.OnUseItem(item);
                        item?.DamageDurability(durabilityDamage);
                    }
                    else if (use.ActionType == ItemUseInventoryTransactionActionType.Place) {
                        bool placed = UseItemOnBlock(player, inventory, item, use);
                        if (legacyRequestId != 0) {
                            ItemStack? remaining = inventory.Inventory.GetItem(slot);
                            player.Network.SendPacket(player, new ItemStackResponsePacket {
                                Responses = [new ItemStackResponseInfo {
                                    ClientRequestId = legacyRequestId,
                                    Result = placed ? ItemStackNetResult.Success : ItemStackNetResult.Error,
                                    Containers = placed ? [new ItemStackResponseContainerInfo {
                                        ContainerName = inventory.Inventory.GetFullContainerName(),
                                        Slots = [new ItemStackResponseSlotInfo {
                                            RequestedSlot = (byte)slot,
                                            Slot = (byte)slot,
                                            Amount = (byte)(remaining?.StackSize ?? 0),
                                            ItemStackNetId = remaining?.NetId
                                        }]
                                    }] : null
                                }]
                            });
                        }
                    }
                    break;
                case ItemUseOnActorInventoryTransaction actor:
                    if (actor.ActionType is not (ItemUseOnActorInventoryTransactionActionType.Interact or
                        ItemUseOnActorInventoryTransactionActionType.ItemInteract or
                        ItemUseOnActorInventoryTransactionActionType.Attack) ||
                        !float.IsFinite(actor.HitPosition.X) || !float.IsFinite(actor.HitPosition.Y) ||
                        !float.IsFinite(actor.HitPosition.Z)) {
                        if (attack)
                            Logger.Scoped("Network").Info($"Rejected attack from {player.Username}: invalid hit position.");
                        return;
                    }

                    Entity? target = player.Dimension.GetEntity(actor.RuntimeId);
                    if (target is null || target.Dead || ReferenceEquals(target, player) ||
                        target is Player targetPlayer && (targetPlayer.Disposed || !targetPlayer.Spawned ||
                        !ReferenceEquals(targetPlayer.Dimension, player.Dimension))) {
                        if (attack)
                            Logger.Scoped("Network").Info($"Rejected attack from {player.Username}: target {actor.RuntimeId} is missing or unavailable.");
                        return;
                    }

                    float targetWidth = target.Type?.PropertiesPayload?.Components.CollisionBox?.Width is double width &&
                        double.IsFinite(width) && width > 0 && width <= float.MaxValue
                            ? (float)width
                            : 0.6f;
                    float targetHeight = target.Type?.PropertiesPayload?.Components.CollisionBox?.Height is double height &&
                        double.IsFinite(height) && height > 0 && height <= float.MaxValue
                            ? (float)height
                            : 1.8f;
                    float eyeX = player.Position.X;
                    float eyeY = player.Position.Y + player.HeadOffset;
                    float eyeZ = player.Position.Z;
                    float closestX = Math.Clamp(eyeX, target.Position.X - targetWidth * 0.5f, target.Position.X + targetWidth * 0.5f);
                    float closestY = Math.Clamp(eyeY, target.Position.Y, target.Position.Y + targetHeight);
                    float closestZ = Math.Clamp(eyeZ, target.Position.Z - targetWidth * 0.5f, target.Position.Z + targetWidth * 0.5f);
                    float distanceX = eyeX - closestX;
                    float distanceY = eyeY - closestY;
                    float distanceZ = eyeZ - closestZ;
                    float distanceSquared = distanceX * distanceX + distanceY * distanceY + distanceZ * distanceZ;
                    float reach = player.InputMode == InputMode.Touch ? 6 : 3;
                    if (!float.IsFinite(distanceSquared) || distanceSquared > reach * reach) {
                        if (attack)
                            Logger.Scoped("Network").Info($"Rejected attack from {player.Username}: target {actor.RuntimeId} is out of reach ({MathF.Sqrt(distanceSquared):0.##} blocks, {reach} block limit for {player.InputMode} input).");
                        return;
                    }

                    if (attack) {
                        int durabilityDamage = item?.Type.AttackDurabilityDamage ?? 0;
                        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
                            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.Attack, durabilityDamage);
                            player.Network.Server.Events.Emit(itemUse);
                            durabilityDamage = itemUse.DurabilityDamage;
                            if (itemUse.IsCanceled()) {
                                item?.DamageDurability(durabilityDamage);
                                break;
                            }
                        }
                        player.OnAttack(target, item);
                        if (!player.Dead && target.GetTrait<EntityHealthTrait>() is { } health) {
                            float damage = (player.Attributes.Get("minecraft:attack_damage")?.Current ?? 1) + (item?.Type.AttackDamageBonus ?? 0);
                            health.ApplyDamage(damage, EntityDamageCause.Entity, player);
                        }
                        item?.DamageDurability(durabilityDamage);
                    }
                    else if (actor.ActionType == ItemUseOnActorInventoryTransactionActionType.Interact)
                        player.OnUseItemOnEntity(item, target, actor.HitPosition);
                    if (transaction is ItemUseOnActorInventoryTransaction {
                        ActionType: ItemUseOnActorInventoryTransactionActionType.ItemInteract
                    }) {
                        int durabilityDamage = item?.Type.MaxDurability > 0 ? 1 : 0;
                        if (player.Network.Server.Events.HasSubscribers<PlayerItemUseEvent>()) {
                            PlayerItemUseEvent itemUse = new(player, item, PlayerItemUseAction.UseOnEntity, durabilityDamage);
                            player.Network.Server.Events.Emit(itemUse);
                            durabilityDamage = itemUse.DurabilityDamage;
                            if (itemUse.IsCanceled()) {
                                item?.DamageDurability(durabilityDamage);
                                break;
                            }
                        }
                        player.OnUseItemOnEntity(item, target, actor.HitPosition);
                        item?.DamageDurability(durabilityDamage);
                    }
                    break;
            }
        }
    }

    private static bool EquipArmorFromLegacyUse(
        Player player,
        PlayerInventoryTrait inventory,
        ItemUseInventoryTransaction transaction,
        int legacyRequestId,
        LegacySetSlot[]? legacySetItemSlots
    ) {
        if (legacyRequestId == 0 || legacySetItemSlots is null)
            return false;

        byte? requestedArmorSlot = null;
        foreach (LegacySetSlot legacySlot in legacySetItemSlots) {
            if (legacySlot.ContainerEnum != ContainerEnumName.Armor)
                continue;
            if (legacySlot.Slots.Length != 1 || requestedArmorSlot.HasValue)
                return false;
            requestedArmorSlot = legacySlot.Slots[0];
        }

        if (requestedArmorSlot is not byte armorSlot)
            return false;

        EntityArmorTrait? armor = player.GetTrait<EntityArmorTrait>();
        InventoryAction[] actions = transaction.Actions.Actions;
        if (armor is null || transaction.ActionType != ItemUseInventoryTransactionActionType.Destroy ||
            transaction.Hand != HandSlot.Mainhand || transaction.Slot < 0 ||
            (uint)transaction.Slot >= inventory.Inventory.ContainerSize || actions.Length != 1 ||
            armorSlot >= armor.Armor.ContainerSize)
            return Reject();

        InventoryAction action = actions[0];
        ItemStack? item = inventory.Inventory.GetItem(transaction.Slot);
        ItemStack? equipped = armor.Armor.GetItem(armorSlot);
        NetworkItemStackDescriptor empty = new();
        NetworkItemStackDescriptor replaced = equipped?.ToNetworkStackDescriptor() ?? empty;
        bool valid = action.Source.Type == InventorySourceType.ContainerInventory &&
            action.Source.ContainerId == (sbyte)inventory.Inventory.ContainerId &&
            action.Slot == (uint)transaction.Slot && item?.Type.ArmorSlot == (ArmorSlot)armorSlot &&
            inventory.MatchesItem(transaction.Slot, transaction.Item, out ItemStack? usedItem) &&
            ReferenceEquals(item, usedItem) &&
            MatchesStack(action.FromItem, item.ToNetworkStackDescriptor()) &&
            MatchesStack(action.ToItem, replaced) &&
            transaction.Item.Id == item.Type.NetworkId && transaction.Item.StackSize == item.StackSize;

        if (!valid || !armor.Equip(inventory, transaction.Slot))
            return Reject();

        equipped = armor.Armor.GetItem(armorSlot);
        player.Network.SendPacket(player, new ItemStackResponsePacket {
            Responses = [new ItemStackResponseInfo {
                ClientRequestId = legacyRequestId,
                Result = ItemStackNetResult.Success,
                Containers = [
                    new ItemStackResponseContainerInfo {
                        ContainerName = inventory.Inventory.GetFullContainerName(),
                        Slots = [new ItemStackResponseSlotInfo {
                            RequestedSlot = checked((byte)transaction.Slot),
                            Slot = checked((byte)inventory.Inventory.GetNetworkSlot(transaction.Slot)),
                            Amount = checked((byte)(inventory.Inventory.GetItem(transaction.Slot)?.StackSize ?? 0)),
                            ItemStackNetId = inventory.Inventory.GetItem(transaction.Slot)?.NetId
                        }]
                    },
                    new ItemStackResponseContainerInfo {
                        ContainerName = armor.Armor.GetFullContainerName(),
                        Slots = [new ItemStackResponseSlotInfo {
                            RequestedSlot = armorSlot,
                            Slot = checked((byte)armor.Armor.GetNetworkSlot(armorSlot)),
                            Amount = checked((byte)(equipped?.StackSize ?? 0)),
                            ItemStackNetId = equipped?.NetId
                        }]
                    }
                ]
            }]
        });
        return true;

        bool Reject() {
            player.Network.SendPacket(player, new ItemStackResponsePacket {
                Responses = [new ItemStackResponseInfo {
                    ClientRequestId = legacyRequestId,
                    Result = ItemStackNetResult.Error
                }]
            });
            inventory.Inventory.Update();
            armor?.Armor.Update();
            return true;
        }
    }

    private static bool EquipArmor(Player player, PlayerInventoryTrait inventory, InventoryAction[] actions) {
        if (!actions.Any(static action => action.Source.ContainerId == (sbyte)ContainerId.Armor))
            return false;

        EntityArmorTrait? armor = player.GetTrait<EntityArmorTrait>();
        InventoryAction? inventoryAction = null;
        InventoryAction? armorAction = null;
        bool valid = armor is not null && actions.Length == 2;
        foreach (InventoryAction action in actions) {
            if (action.Source.Type != InventorySourceType.ContainerInventory) {
                valid = false;
                continue;
            }

            if (action.Source.ContainerId == (sbyte)inventory.Inventory.ContainerId && inventoryAction is null)
                inventoryAction = action;
            else if (action.Source.ContainerId == (sbyte)ContainerId.Armor && armorAction is null)
                armorAction = action;
            else
                valid = false;
        }

        valid = valid && inventoryAction is not null && armorAction is not null &&
            inventoryAction.Slot < inventory.Inventory.ContainerSize &&
            armorAction.Slot < armor!.Armor.ContainerSize;

        if (valid) {
            int inventorySlot = (int)inventoryAction!.Slot;
            int armorSlot = (int)armorAction!.Slot;
            ItemStack? item = inventory.Inventory.GetItem(inventorySlot);
            ItemStack? equipped = armor!.Armor.GetItem(armorSlot);
            NetworkItemStackDescriptor empty = new();

            valid = item?.Type.ArmorSlot == (ArmorSlot)armorSlot &&
                MatchesStack(inventoryAction.FromItem, item?.ToNetworkStackDescriptor() ?? empty) &&
                MatchesStack(inventoryAction.ToItem, equipped?.ToNetworkStackDescriptor() ?? empty) &&
                MatchesStack(armorAction.FromItem, equipped?.ToNetworkStackDescriptor() ?? empty) &&
                MatchesStack(armorAction.ToItem, item?.ToNetworkStackDescriptor() ?? empty);

            if (valid)
                valid = armor.Equip(inventory, inventorySlot);
        }

        if (!valid) {
            inventory.Inventory.Update();
            armor?.Armor.Update();
        }

        return true;
    }

    private static bool MatchesStack(NetworkItemStackDescriptor actual, NetworkItemStackDescriptor expected) =>
        actual.Id == expected.Id && actual.StackSize == expected.StackSize &&
        actual.AuxValue == expected.AuxValue && actual.BlockRuntimeId == expected.BlockRuntimeId &&
        actual.UserData == expected.UserData;

    private static void DropWorldItem(Player player, PlayerInventoryTrait inventory, InventoryAction[] actions, int legacyRequestId) {
        InventoryAction? worldAction = null;
        InventoryAction? inventoryAction = null;
        bool invalidAction = false;
        foreach (InventoryAction action in actions) {
            if (action.Source.Type == InventorySourceType.WorldInteraction) {
                if (worldAction is not null) {
                    invalidAction = true;
                    break;
                }
                worldAction = action;
            }
            else if (action.Source.Type == InventorySourceType.ContainerInventory &&
                action.Source.ContainerId == (sbyte)inventory.Inventory.ContainerId) {
                if (inventoryAction is not null) {
                    invalidAction = true;
                    break;
                }
                inventoryAction = action;
            }
            else {
                invalidAction = true;
                break;
            }
        }

        ItemStack? item = null;
        bool valid = !invalidAction && actions.Length == 2 && worldAction is not null && inventoryAction is not null &&
            inventoryAction.Slot < 9 &&
            inventory.MatchesItem((int)inventoryAction.Slot, inventoryAction.FromItem, out item) && item is not null;
        int amount = valid ? inventoryAction!.FromItem.StackSize - inventoryAction.ToItem.StackSize : 0;
        if (valid) {
            NetworkItemStackDescriptor from = inventoryAction!.FromItem;
            NetworkItemStackDescriptor dropped = worldAction!.ToItem;
            valid = amount > 0 && amount <= item!.StackSize &&
                SameItem(from, item.ToNetworkStackDescriptor()) &&
                (inventoryAction.ToItem.StackSize == 0 || SameItem(from, inventoryAction.ToItem)) &&
                dropped.Id == from.Id && dropped.AuxValue == from.AuxValue && dropped.StackSize == amount &&
                dropped.BlockRuntimeId == from.BlockRuntimeId && dropped.UserData == from.UserData;
        }

        if (!valid) {
            Logger.Scoped("Network").Warn($"Rejected item drop from {player.Username}: invalid inventory transaction.");
            inventory.Inventory.Update();
            if (legacyRequestId != 0) {
                player.Network.SendPacket(player, new ItemStackResponsePacket {
                    Responses = [new ItemStackResponseInfo {
                        ClientRequestId = legacyRequestId,
                        Result = ItemStackNetResult.Error
                    }]
                });
            }
            return;
        }

        ItemStack sourceItem = item!;
        InventoryAction sourceAction = inventoryAction!;
        InventoryAction dropAction = worldAction!;
        ItemStack drop = sourceItem.Clone((ushort)amount, cloneNbt: amount != sourceItem.StackSize);
        ItemEntity droppedEntity = new(
            drop,
            player,
            dropAction.Source.Flags == InventorySourceFlags.WorldInteractionRandom
        );
        if (inventory.Inventory.TakeItem((int)sourceAction.Slot, (ushort)amount) is null) {
            Logger.Scoped("Network").Warn($"Rejected item drop from {player.Username}: source slot was empty.");
            inventory.Inventory.Update();
            if (legacyRequestId != 0) {
                player.Network.SendPacket(player, new ItemStackResponsePacket {
                    Responses = [new ItemStackResponseInfo {
                        ClientRequestId = legacyRequestId,
                        Result = ItemStackNetResult.Error
                    }]
                });
            }
            return;
        }
        droppedEntity.Spawn(player.Dimension);

        if (legacyRequestId != 0) {
            ItemStack? remaining = inventory.Inventory.GetItem((int)sourceAction.Slot);
            player.Network.SendPacket(player, new ItemStackResponsePacket {
                Responses = [new ItemStackResponseInfo {
                    ClientRequestId = legacyRequestId,
                    Result = ItemStackNetResult.Success,
                    Containers = [new ItemStackResponseContainerInfo {
                        ContainerName = inventory.Inventory.GetFullContainerName(),
                        Slots = [new ItemStackResponseSlotInfo {
                            RequestedSlot = (byte)sourceAction.Slot,
                            Slot = (byte)sourceAction.Slot,
                            Amount = (byte)(remaining?.StackSize ?? 0),
                            ItemStackNetId = remaining?.NetId
                        }]
                    }]
                }]
            });
        }
    }

    private static bool SameItem(NetworkItemStackDescriptor first, NetworkItemStackDescriptor second) =>
        first.Id == second.Id && first.AuxValue == second.AuxValue &&
        first.BlockRuntimeId == second.BlockRuntimeId && first.UserData == second.UserData;

    private static bool UseItemOnBlock(Player player, PlayerInventoryTrait inventory, ItemStack? item, ItemUseInventoryTransaction use) {
        PlayerChunkRenderingTrait? rendering = player.GetTrait<PlayerChunkRenderingTrait>();
        if (use.Face > 5 || use.Position.Y < -64 || use.Position.Y >= 320 ||
            use.ClientInteractPrediction is not (ItemUseInventoryTransactionPredictedResult.Failure or
                ItemUseInventoryTransactionPredictedResult.Success) ||
            !float.IsFinite(use.ClickPosition.X) || !float.IsFinite(use.ClickPosition.Y) ||
            !float.IsFinite(use.ClickPosition.Z) ||
            use.ClickPosition.X < 0 || use.ClickPosition.X > 1 ||
            use.ClickPosition.Y < 0 || use.ClickPosition.Y > 1 ||
            use.ClickPosition.Z < 0 || use.ClickPosition.Z > 1 ||
            rendering is null || !rendering.Viewing(use.Position.X >> 4, use.Position.Z >> 4)) {
            RepairPlacement(player, use);
            inventory.Inventory.UpdateSlot(use.Slot);
            return false;
        }

        float reachX = use.Position.X + use.ClickPosition.X - player.Position.X;
        float reachY = use.Position.Y + use.ClickPosition.Y - (player.Position.Y + player.HeadOffset);
        float reachZ = use.Position.Z + use.ClickPosition.Z - player.Position.Z;
        var clickedChunk = player.Dimension.GetChunk(use.Position.X >> 4, use.Position.Z >> 4);
        if (reachX * reachX + reachY * reachY + reachZ * reachZ > BlockReachSquared || clickedChunk is null) {
            RepairPlacement(player, use);
            inventory.Inventory.UpdateSlot(use.Slot);
            return false;
        }

        BlockPermutation clicked = clickedChunk.GetPermutation(use.Position.X & 15, use.Position.Y, use.Position.Z & 15);
        if (unchecked((uint)clicked.Hash) != use.TargetBlockId || clicked.Identifier == "minecraft:air") {
            RepairPlacement(player, use);
            inventory.Inventory.UpdateSlot(use.Slot);
            return false;
        }

        if (use.ClientInteractPrediction == ItemUseInventoryTransactionPredictedResult.Success &&
            !player.Flags.Get(ActorFlag.Sneaking)) {
            if (player.Dimension.GetBlockActor(clickedChunk, use.Position.X & 15, use.Position.Y, use.Position.Z & 15) is { } actor) {
                actor.OnInteract(player);
                return true;
            }
            if (BlockPalette.OnInteract(clicked.Identifier, player, use.Position))
                return true;
        }

        BlockPermutation? permutation = item?.Type.PlacementPermutation;
        if (use.ClientInteractPrediction == ItemUseInventoryTransactionPredictedResult.Failure || permutation is null) {
            if (use.ClientInteractPrediction == ItemUseInventoryTransactionPredictedResult.Success) {
                RepairPlacement(player, use);
                inventory.Inventory.UpdateSlot(use.Slot);
            }
            player.OnUseItemOnBlock(item, clicked, use.Position, use.Face, use.ClickPosition);
            return false;
        }

        int x = use.Position.X;
        int y = use.Position.Y;
        int z = use.Position.Z;
        string placedHalf = use.Face == 0 || (use.Face != 1 && use.ClickPosition.Y > 0.5f) ? "top" : "bottom";
        string mergedHalf = use.Face == 1 || (use.Face != 0 && use.ClickPosition.Y > 0.5f) ? "top" : "bottom";
        bool mergingSlab = false;
        if (!Replaceable(clicked)) {
            if (clicked.Identifier == permutation.Identifier &&
                BlockPalette.FindDoubleSlab(permutation.Identifier, mergedHalf) is { } doubleSlab &&
                clicked.State.Values.TryGetValue("minecraft:vertical_half", out object? clickedHalf) &&
                clickedHalf is string clickedHalfName && clickedHalfName != mergedHalf) {
                permutation = doubleSlab;
                mergingSlab = true;
            }
            else {
                switch (use.Face) {
                    case 0: y--; break;
                    case 1: y++; break;
                    case 2: z--; break;
                    case 3: z++; break;
                    case 4: x--; break;
                    case 5: x++; break;
                }
            }
        }

        var chunk = player.Dimension.GetChunk(x >> 4, z >> 4);
        if (!player.Abilities.Get(PlayerAbility.Build) || y < -64 || y >= 320 ||
            x < -30_000_000 || x >= 30_000_000 || z < -30_000_000 || z >= 30_000_000 ||
            chunk is null || !rendering.Viewing(x >> 4, z >> 4) ||
            permutation.Identifier == "minecraft:air" ||
            permutation.State.Values.ContainsKey("upper_block_bit") ||
            permutation.State.Values.ContainsKey("head_piece_bit")) {
            RepairPlacement(player, use);
            inventory.Inventory.UpdateSlot(use.Slot);
            return false;
        }

        BlockPermutation existing = chunk.GetPermutation(x & 15, y, z & 15);
        if (!mergingSlab && (!Replaceable(existing) || existing.Identifier == permutation.Identifier)) {
            RepairPlacement(player, use);
            inventory.Inventory.UpdateSlot(use.Slot);
            return false;
        }

        BlockType? blockType = BlockPalette.FindType(permutation.Identifier);
        bool cornerableStairs = BlockPalette.IsCornerableStairs(permutation.Identifier);
        if (BlockPalette.IsSlab(permutation.Identifier))
            permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "minecraft:vertical_half", placedHalf) ?? permutation;
        if (cornerableStairs) {
            int direction = StairDirection(player.Rotation.Y);
            bool upsideDown = placedHalf == "top";
            permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "weirdo_direction", direction) ?? permutation;
            permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "upside_down_bit", upsideDown) ?? permutation;
        }

        if (blockType?.Solid == true) {
            bool collision = Collides(player, x, y, z);
            foreach (Player viewer in player.Dimension.GetChunkViewers(x >> 4, z >> 4)) {
                if (viewer.Spawned && !viewer.Disposed && !ReferenceEquals(viewer, player) && Collides(viewer, x, y, z)) {
                    collision = true;
                    break;
                }
            }
            if (collision) {
                RepairPlacement(player, use);
                inventory.Inventory.UpdateSlot(use.Slot);
                return false;
            }
        }

        if (permutation.State.Values.ContainsKey("pillar_axis")) {
            string axis = use.Face is 4 or 5 ? "x" : use.Face is 2 or 3 ? "z" : "y";
            permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "pillar_axis", axis) ?? permutation;
        }
        if (blockType is not null && Array.IndexOf(blockType.States, "minecraft:cardinal_direction") >= 0) {
            int facing = ((int)MathF.Floor((player.Rotation.Y + 45) / 90) & 3);
            string direction = facing switch {
                0 => "north",
                1 => "east",
                2 => "south",
                _ => "west"
            };
            permutation = BlockPalette.Find(
                permutation.Identifier,
                permutation.State,
                "minecraft:cardinal_direction",
                direction
            ) ?? permutation;
        }

        player.Dimension.RemoveBlockActor(x, y, z);
        chunk.SetPermutation(x & 15, y, z & 15, permutation);
        BlockPalette.OnNeighborUpdate(player.Dimension, x, y, z);
        Logger.Scoped("Network").Debug($"Player {player.Username} placed {permutation.Identifier} at ({x}, {y}, {z}) in {player.Dimension.Identifier}.");
        try {
            if (!player.Abilities.Get(PlayerAbility.InstantBuild)) {
                item!.DecrementStack();
                if (use.Item.NetIdVariant is int predictedNetId)
                    inventory.RememberClientRequestNetId(inventory.Inventory, use.Slot, predictedNetId, inventory.Inventory.GetItem(use.Slot));
            }
        }
        finally {
            SendBlockUpdate(player, x, y, z, 0, broadcast: true);
            player.Dimension.UpdateNeighbors(x, y, z);
        }
        player.Dimension.PlaceBlockActor(x, y, z, permutation.Identifier)?.Update();
        return true;
    }

    private static int StairDirection(float yaw) {
        int direction = (int)MathF.Floor((yaw + 45) / 90) & 3;
        return direction switch {
            0 => 2,
            1 => 1,
            2 => 3,
            _ => 0
        };
    }

    private static bool Replaceable(BlockPermutation block) => block.Identifier is
        "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air" or
        "minecraft:water" or "minecraft:flowing_water" or "minecraft:lava" or "minecraft:flowing_lava" or
        "minecraft:short_grass" or "minecraft:tall_grass" or "minecraft:fern" or "minecraft:large_fern" or
        "minecraft:dead_bush" or "minecraft:vine" or "minecraft:seagrass" or "minecraft:tall_seagrass" or
        "minecraft:snow_layer" or "minecraft:fire";

    private static bool Collides(Player player, int x, int y, int z) {
        bool lowPose = player.Flags.Get(ActorFlag.Swimming) || player.Flags.Get(ActorFlag.Gliding) || player.Flags.Get(ActorFlag.Crawling);
        bool sneaking = player.Flags.Get(ActorFlag.Sneaking);
        float feet = player.Position.Y;
        float height = lowPose ? 0.6f : sneaking ? 1.5f : 1.8f;
        return player.Position.X + 0.3f > x && player.Position.X - 0.3f < x + 1 &&
            player.Position.Z + 0.3f > z && player.Position.Z - 0.3f < z + 1 &&
            feet + height > y && feet < y + 1;
    }

    private static void RepairPlacement(Player player, ItemUseInventoryTransaction use) {
        if (use.Face > 5)
            return;
        float dx = use.Position.X + 0.5f - player.Position.X;
        float dy = use.Position.Y + 0.5f - (player.Position.Y + player.HeadOffset);
        float dz = use.Position.Z + 0.5f - player.Position.Z;
        if (dx * dx + dy * dy + dz * dz > 8 * 8)
            return;

        int x = use.Position.X;
        int y = use.Position.Y;
        int z = use.Position.Z;
        for (uint layer = 0; layer < 2; layer++)
            SendBlockUpdate(player, x, y, z, layer, broadcast: false);
        switch (use.Face) {
            case 0: y--; break;
            case 1: y++; break;
            case 2: z--; break;
            case 3: z++; break;
            case 4: x--; break;
            case 5: x++; break;
        }
        for (uint layer = 0; layer < 2; layer++)
            SendBlockUpdate(player, x, y, z, layer, broadcast: false);
    }

    private static void SendBlockUpdate(Player player, int x, int y, int z, uint layer, bool broadcast, bool playPlaceSound = true) {
        if (y < -64 || y >= 320 || player.Dimension.GetChunk(x >> 4, z >> 4) is not { } chunk ||
            (!broadcast && player.GetTrait<PlayerChunkRenderingTrait>()?.Viewing(x >> 4, z >> 4) != true))
            return;

        UpdateBlockPacket update = blockUpdate ?? new();
        blockUpdate = null;
        try {
            update.Position.X = x;
            update.Position.Y = y;
            update.Position.Z = z;
            BlockPermutation block = chunk.GetPermutation(x & 15, y, z & 15, (int)layer);
            update.BlockRuntimeId = unchecked((uint)block.NetworkId);
            update.Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network;
            update.Layer = layer;
            if (broadcast && playPlaceSound)
                player.Dimension.Broadcast(update, x >> 4, z >> 4, additionalPacket: new LevelSoundEventPacket {
                    SoundEvent = "place",
                    Position = new Vec3 { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f },
                    Data = block.NetworkId,
                    ActorUniqueId = -1
                });
            else if (broadcast)
                player.Dimension.Broadcast(update, x >> 4, z >> 4);
            else
                player.Network.SendPacket(player, update);
        }
        finally {
            blockUpdate ??= update;
        }
    }
}
