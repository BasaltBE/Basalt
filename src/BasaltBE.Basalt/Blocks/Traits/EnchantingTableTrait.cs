using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:enchanting_table")]
public sealed class EnchantingTableTrait : BlockTrait {
    public override bool OnInteract(Player player, BlockPos position) {
        EnchantingTableContainer container = new(player, position);
        container.Changed += container.Refresh;
        container.LastViewerClosed += container.ReturnItems;
        if (!container.Open(player))
            return false;
        container.Refresh();
        return true;
    }
}

internal sealed class EnchantingTableContainer : BlockContainer {
    private readonly Player player;
    private readonly int bookshelfCount;
    private readonly Random random = new();
    private EnchantingOffer[] offers = [];
    private ItemStack? offeredItem;
    private bool optionsSent;

    public EnchantingTableContainer(Player player, BlockPos position)
        : base(
            position,
            2,
            ContainerId.First,
            ContainerEnumName.EnchantingInput,
            ContainerType.Enchantment,
            slotOffset: 14,
            slotNames: [ContainerEnumName.EnchantingInput, ContainerEnumName.EnchantingMaterial]
        ) {
        this.player = player;
        bookshelfCount = CountBookshelves(position, player);
    }

    public override bool CanContain(int slot, ItemStack item) {
        if (slot == 1)
            return item.Type.Identifier == "minecraft:lapis_lazuli";
        if (slot != 0 || item.StackSize != 1)
            return false;
        if (item.Type.Identifier is "minecraft:book" or "minecraft:enchanted_book")
            return true;

        foreach (var enchantment in player.Network.Server.Enchantments.Values) {
            if (enchantment.BedrockId is short && enchantment.AppliesTo(item.Type) &&
                item.Type.PropertiesPayload.Components.Contains("minecraft:enchantable"))
                return true;
        }
        return false;
    }

    public void Refresh() {
        if (!ReferenceEquals(player.OpenedContainer, this))
            return;

        ItemStack? item = GetItem(0);
        if (item is null) {
            if (offeredItem is null && optionsSent)
                return;
            offers = [];
            offeredItem = null;
            optionsSent = true;
            player.SendPacket(new PlayerEnchantOptionsPacket());
            return;
        }
        if (offeredItem?.CanStackWith(item) == true && optionsSent)
            return;

        offeredItem = item.Clone();
        if (item.Enchanted) {
            offers = [];
            optionsSent = true;
            player.SendPacket(new PlayerEnchantOptionsPacket());
            return;
        }

        bool isBook = item.Type.Identifier == "minecraft:book";
        List<Enchantment> eligible = [];
        foreach (Enchantment enchantment in player.Network.Server.Enchantments.Values) {
            if (enchantment.BedrockId is not short || (!isBook && !enchantment.AppliesTo(item.Type)))
                continue;

            bool conflict = false;
            foreach (string identifier in enchantment.IncompatibleWith.Span)
                conflict |= item.GetEnchantmentLevel(identifier) > 0;
            if (!conflict)
                eligible.Add(enchantment);
        }

        if (eligible.Count == 0 || !isBook && !item.Type.PropertiesPayload.Components.Contains("minecraft:enchantable")) {
            offers = [];
            optionsSent = true;
            player.SendPacket(new PlayerEnchantOptionsPacket());
            return;
        }

        List<EnchantingOffer> currentOffers = [];
        List<ItemEnchantOption> options = [];
        for (int index = 0; index < 3; index++) {
            int power = random.Next(8) + 1 + (bookshelfCount >> 1) + random.Next(bookshelfCount + 1);
            int cost = index switch {
                0 => Math.Max(power / 3, 1),
                1 => power * 2 / 3 + 1,
                _ => Math.Max(power, bookshelfCount * 2)
            };
            if (cost < index + 1)
                continue;

            power += 1 + random.Next(item.Type.Enchantability / 4 + 1) + random.Next(item.Type.Enchantability / 4 + 1);
            float randomSpan = (random.NextSingle() + random.NextSingle() - 1) * 0.15f;
            power = Math.Max(1, (int)MathF.Round(power + power * randomSpan));

            Enchantment enchantment = eligible[random.Next(eligible.Count)];
            byte level = checked((byte)Math.Clamp((power + 5) / 10, 1, enchantment.MaximumLevel));
            uint recipeId = unchecked((uint)random.Next(1, int.MaxValue));
            byte spendLevels = checked((byte)(index + 1));
            currentOffers.Add(new EnchantingOffer(recipeId, checked((byte)cost), spendLevels, enchantment, level));
            EnchantmentInstance instance = new() {
                Type = (EnchantType)enchantment.BedrockId!.Value,
                Level = level
            };
            EnchantmentInstance[][] enchantmentGroups = [[], [], []];
            int group = enchantment.BedrockId switch {
                0 => 0,
                15 => 2,
                _ => 1
            };
            enchantmentGroups[group] = [instance];
            options.Add(new ItemEnchantOption {
                Cost = checked((byte)cost),
                Enchants = new ItemEnchants {
                    Slot = 0,
                    Enchantments = enchantmentGroups
                },
                Name = enchantment.Identifier,
                EnchantNetId = recipeId
            });
        }
        offers = [.. currentOffers];
        optionsSent = true;
        player.SendPacket(new PlayerEnchantOptionsPacket { Options = [.. options] });
    }

    internal ItemStackResponseInfo ProcessRequest(ItemStackRequestData request, out string? failureReason) {
        failureReason = null;
        if (!ReferenceEquals(player.OpenedContainer, this)) {
            failureReason = "enchanting table is not open";
            return Error(request.ClientRequestId);
        }

        CraftRecipeActionData? selected = null;
        bool resultPlaced = false;
        foreach (ItemStackRequestActionData action in request.Actions) {
            if (action is CraftRecipeActionData craft) {
                if (selected is not null) {
                    failureReason = "request contains multiple recipe actions";
                    return Error(request.ClientRequestId);
                }
                if (craft.NumberOfRequestedCrafts != 1) {
                    failureReason = $"requested crafts: {craft.NumberOfRequestedCrafts}";
                    return Error(request.ClientRequestId);
                }
                selected = craft;
            }
            else if (action is PlaceActionData place) {
                if (resultPlaced || place.Amount != 1 ||
                    place.Source.ContainerName.ContainerName != ContainerEnumName.CreatedOutput || place.Source.Slot != 50 ||
                    place.Source.ContainerName.DynamicId is not null and not 0 ||
                    place.Destination.ContainerName.ContainerName != ContainerEnumName.EnchantingInput ||
                    place.Destination.Slot != GetNetworkSlot(0) ||
                    place.Destination.ContainerName.DynamicId is not null and not 0) {
                    failureReason = "invalid enchanted result transfer";
                    return Error(request.ClientRequestId);
                }
                resultPlaced = true;
            }
            else if (action is not (ConsumeActionData or CreateActionData or CraftResultsActionData)) {
                failureReason = $"unsupported action: {action.ActionType}";
                return Error(request.ClientRequestId);
            }
        }
        if (selected is null) {
            failureReason = "request has no recipe action";
            return Error(request.ClientRequestId);
        }
        if (!resultPlaced) {
            failureReason = "enchant result was not placed into the input slot";
            return Error(request.ClientRequestId);
        }

        EnchantingOffer offer = default;
        bool found = false;
        foreach (EnchantingOffer candidate in offers) {
            if (candidate.Id != unchecked((uint)selected.RecipeNetId))
                continue;
            offer = candidate;
            found = true;
            break;
        }
        ItemStack? item = GetItem(0);
        ItemStack? lapis = GetItem(1);
        PlayerExperienceTrait? experience = player.GetTrait<PlayerExperienceTrait>();
        bool creative = player.GameType == GameType.Creative;
        if (!found) {
            failureReason = $"unknown enchant option: {selected.RecipeNetId}";
            Update();
            return Error(request.ClientRequestId);
        }
        if (item is null) {
            failureReason = "enchanting input is empty";
            Update();
            return Error(request.ClientRequestId);
        }
        if (experience is null) {
            failureReason = "experience trait is missing";
            Update();
            return Error(request.ClientRequestId);
        }
        if (item.Enchanted || item.Type.Identifier != "minecraft:book" && !offer.Enchantment.AppliesTo(item.Type)) {
            failureReason = "item no longer matches the enchant option";
            Update();
            return Error(request.ClientRequestId);
        }
        if (!creative && (lapis?.StackSize ?? 0) < offer.SpendLevels) {
            failureReason = $"not enough lapis: need {offer.SpendLevels}, have {lapis?.StackSize ?? 0}";
            Update();
            return Error(request.ClientRequestId);
        }
        if (!creative && experience.Level < offer.Cost) {
            failureReason = $"not enough experience: need level {offer.Cost}, have {experience.Level}";
            Update();
            return Error(request.ClientRequestId);
        }

        foreach (string identifier in offer.Enchantment.IncompatibleWith.Span) {
            if (item.GetEnchantmentLevel(identifier) > 0) {
                failureReason = "offer conflicts with an item enchantment";
                Update();
                return Error(request.ClientRequestId);
            }
        }

        ItemStack enchanted;
        if (item.Type.Identifier == "minecraft:book") {
            ItemType enchantedBook = ItemPalette.Find("minecraft:enchanted_book")
                ?? throw new InvalidDataException("Enchanted book item type is missing.");
            enchanted = new ItemStack(enchantedBook, 1, item.Metadata, item.Clone().Nbt);
        }
        else {
            enchanted = item.Clone();
        }
        enchanted.AddEnchantment(offer.Enchantment, offer.Level);
        if (!creative && !experience.SpendLevels(offer.SpendLevels))
            return Error(request.ClientRequestId);
        SetItem(0, enchanted);
        if (!creative)
            TakeItem(1, offer.SpendLevels);
        failureReason = null;
        return new ItemStackResponseInfo {
            Result = ItemStackNetResult.Success,
            ClientRequestId = request.ClientRequestId,
            Containers = [
                new ItemStackResponseContainerInfo {
                    ContainerName = { ContainerName = ContainerEnumName.CreatedOutput, DynamicId = 0 },
                    Slots = [ResponseSlot(50, null)]
                },
                new ItemStackResponseContainerInfo {
                    ContainerName = { ContainerName = ContainerEnumName.EnchantingInput },
                    Slots = [ResponseSlot(GetNetworkSlot(0), GetItem(0))]
                },
                new ItemStackResponseContainerInfo {
                    ContainerName = { ContainerName = ContainerEnumName.EnchantingMaterial },
                    Slots = [ResponseSlot(GetNetworkSlot(1), GetItem(1))]
                }
            ]
        };
    }

    public void ReturnItems() {
        offers = [];
        if (player.GetTrait<PlayerInventoryTrait>() is not { } inventory)
            return;

        for (int slot = 0; slot < ContainerSize; slot++) {
            if (TakeItem(slot, ushort.MaxValue) is not { } item)
                continue;
            if (inventory.Inventory.AddItem(item) is { } remaining)
                new ItemEntity(remaining, player, Position.X, Position.Y, Position.Z).Spawn(player.Dimension);
        }
    }

    private static ItemStackResponseInfo Error(int requestId) => new() {
        Result = ItemStackNetResult.Error,
        ClientRequestId = requestId
    };

    private static ItemStackResponseSlotInfo ResponseSlot(int slot, ItemStack? item) => new() {
        RequestedSlot = (byte)slot,
        Slot = (byte)slot,
        Amount = (byte)(item?.StackSize ?? 0),
        ItemStackNetId = item?.NetId
    };

    private static int CountBookshelves(BlockPos position, Player player) {
        int count = 0;
        for (int xOffset = -2; xOffset <= 2; xOffset++) {
            for (int zOffset = -2; zOffset <= 2; zOffset++) {
                if (Math.Max(Math.Abs(xOffset), Math.Abs(zOffset)) != 2)
                    continue;
                int gapX = position.X + Math.Sign(xOffset);
                int gapZ = position.Z + Math.Sign(zOffset);
                if (player.Dimension.GetPermutation(gapX, position.Y + 1, gapZ).Identifier != "minecraft:air")
                    continue;
                int x = position.X + xOffset;
                int z = position.Z + zOffset;
                for (int yOffset = 0; yOffset <= 1; yOffset++)
                    if (player.Dimension.GetPermutation(x, position.Y + yOffset, z).Identifier == "minecraft:bookshelf")
                        count++;
            }
        }
        return Math.Min(count, 15);
    }

    private readonly record struct EnchantingOffer(uint Id, byte Cost, byte SpendLevels, Enchantment Enchantment, byte Level);
}
