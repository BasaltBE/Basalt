using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class InventoryTransactionHandler {
    private const float BlockReachSquared = 6 * 6;
    private const float EntityReachSquared = 4 * 4;
    [ThreadStatic]
    private static UpdateBlockPacket? blockUpdate;

    public void Handle(NethernetPeer peer, Player? player, InventoryTransactionPacket packet) {
        if (player is null)
            throw new InvalidDataException("Inventory transaction arrived before player login.");
        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (player.Disposed || !player.Spawned)
            return;

        player.GetTrait<PlayerInventoryTrait>()?.QueueTransaction(packet.Transaction, packet.LegacyRequestId);
    }

    internal static void ProcessTransaction(Player player, InventoryTransactionData transaction, int legacyRequestId) {
        lock (player.Sync) {
            PlayerInventoryTrait? inventory = player.GetTrait<PlayerInventoryTrait>();
            if (player.Disposed || !player.Spawned || inventory is null)
                return;

            if (legacyRequestId != 0 && transaction is not ItemUseInventoryTransaction) {
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
            if (descriptor is null || origin is null || hand != HandSlot.Mainhand ||
                (uint)slot >= 9 || slot != inventory.SelectedSlot ||
                !float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || !float.IsFinite(origin.Z)) {
                return;
            }
            if (!inventory.MatchesItem(slot, descriptor, out var item)) {
                if (transaction is ItemUseInventoryTransaction { ActionType: ItemUseInventoryTransactionActionType.Place } predicted)
                    RepairPlacement(player, predicted);
                inventory.Inventory.UpdateSlot(slot);
                return;
            }

            switch (transaction) {
                case ItemUseInventoryTransaction use:
                    if (use.ActionType == ItemUseInventoryTransactionActionType.Use) {
                        player.OnUseItem(item);
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
                        !float.IsFinite(actor.HitPosition.Z))
                        return;

                    Entity? target = player.Dimension.GetEntity(actor.RuntimeId);
                    if (target is null || ReferenceEquals(target, player) ||
                        target is Player targetPlayer && (targetPlayer.Disposed || !targetPlayer.Spawned ||
                        !ReferenceEquals(targetPlayer.Dimension, player.Dimension)))
                        return;

                    float targetX = target.Position.X - player.Position.X;
                    float targetY = target.Position.Y - player.Position.Y;
                    float targetZ = target.Position.Z - player.Position.Z;
                    float distanceSquared = targetX * targetX + targetY * targetY + targetZ * targetZ;
                    if (!float.IsFinite(distanceSquared) || distanceSquared > EntityReachSquared)
                        return;

                    if (actor.ActionType == ItemUseOnActorInventoryTransactionActionType.Attack)
                        player.OnAttack(target, item);
                    else
                        player.OnUseItemOnEntity(item, target, actor.HitPosition);
                    break;
            }
        }
    }

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
        float reachY = use.Position.Y + use.ClickPosition.Y - player.Position.Y;
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

        BlockPermutation? permutation = item?.Type.PlacementPermutation;
        if (use.ClientInteractPrediction == ItemUseInventoryTransactionPredictedResult.Failure || permutation is null) {
            player.OnUseItemOnBlock(item, clicked, use.Position, use.Face, use.ClickPosition);
            if (use.ClientInteractPrediction == ItemUseInventoryTransactionPredictedResult.Success) {
                RepairPlacement(player, use);
                inventory.Inventory.UpdateSlot(use.Slot);
            }
            return false;
        }

        int x = use.Position.X;
        int y = use.Position.Y;
        int z = use.Position.Z;
        if (!Replaceable(clicked)) {
            switch (use.Face) {
                case 0: y--; break;
                case 1: y++; break;
                case 2: z--; break;
                case 3: z++; break;
                case 4: x--; break;
                case 5: x++; break;
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
        if (!Replaceable(existing) || existing.Identifier == permutation.Identifier) {
            RepairPlacement(player, use);
            inventory.Inventory.UpdateSlot(use.Slot);
            return false;
        }

        if (BlockPalette.FindType(permutation.Identifier)?.Solid == true) {
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

        chunk.SetBlockActor(x & 15, y, z & 15, null);
        chunk.SetPermutation(x & 15, y, z & 15, permutation);
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
        }
        return true;
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
        float feet = player.Position.Y - (lowPose ? 0.4f : sneaking ? 1.27f : 1.62f);
        float height = lowPose ? 0.6f : sneaking ? 1.5f : 1.8f;
        return player.Position.X + 0.3f > x && player.Position.X - 0.3f < x + 1 &&
            player.Position.Z + 0.3f > z && player.Position.Z - 0.3f < z + 1 &&
            feet + height > y && feet < y + 1;
    }

    private static void RepairPlacement(Player player, ItemUseInventoryTransaction use) {
        if (use.Face > 5)
            return;
        float dx = use.Position.X + 0.5f - player.Position.X;
        float dy = use.Position.Y + 0.5f - player.Position.Y;
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

    private static void SendBlockUpdate(Player player, int x, int y, int z, uint layer, bool broadcast) {
        if (y < -64 || y >= 320 || player.Dimension.GetChunk(x >> 4, z >> 4) is not { } chunk ||
            (!broadcast && player.GetTrait<PlayerChunkRenderingTrait>()?.Viewing(x >> 4, z >> 4) != true))
            return;

        UpdateBlockPacket update = blockUpdate ?? new();
        blockUpdate = null;
        try {
            update.Position.X = x;
            update.Position.Y = y;
            update.Position.Z = z;
            update.BlockRuntimeId = unchecked((uint)chunk.GetPermutation(x & 15, y, z & 15, (int)layer).Hash);
            update.Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network;
            update.Layer = layer;
            if (broadcast)
                player.Dimension.Broadcast(update, x >> 4, z >> 4, additionalPacket: new LevelSoundEventPacket {
                    SoundEvent = "place",
                    Position = new Vec3 { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f },
                    Data = unchecked((int)update.BlockRuntimeId),
                    ActorUniqueId = -1
                });
            else
                player.Network.SendPacket(player, update);
        }
        finally {
            blockUpdate ??= update;
        }
    }
}
