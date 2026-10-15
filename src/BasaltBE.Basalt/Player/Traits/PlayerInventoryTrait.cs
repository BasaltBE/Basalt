using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerInventoryTrait : EntityTrait {
    private readonly Dictionary<(Container Container, int Slot), SlotPrediction> slotPredictions = [];

    public readonly EntityContainer Inventory;
    public readonly EntityContainer Cursor;

    public PlayerInventoryTrait(Entity entity) : base(entity) {
        Player player = entity as Player
            ?? throw new ArgumentException("Player inventory traits require a player entity.", nameof(entity));
        Inventory = new EntityContainer(player, 36);
        Cursor = new EntityContainer(
            player,
            1,
            ContainerId.PlayerOnlyUi,
            ContainerEnumName.Cursor,
            ContainerType.Inventory,
            dynamicId: 0
        );
    }

    public override void OnSpawn() {
        // Temp
        Inventory.SetItem(0, new ItemStack("minecraft:stick", 64));
        Inventory.AddPersistent((Player)Entity);
    }

    internal bool MatchesClientRequestNetId(Container container, int slot, int clientRequestNetId, ItemStack? item) =>
        slotPredictions.TryGetValue((container, slot), out SlotPrediction prediction) &&
        prediction.ClientRequestNetId == clientRequestNetId &&
        prediction.ItemNetId == item?.NetId;

    internal void RememberClientRequestNetId(Container container, int slot, int clientRequestNetId, ItemStack? item) =>
        slotPredictions[(container, slot)] = new SlotPrediction(clientRequestNetId, item?.NetId);

    private readonly record struct SlotPrediction(int ClientRequestNetId, int? ItemNetId);
}
